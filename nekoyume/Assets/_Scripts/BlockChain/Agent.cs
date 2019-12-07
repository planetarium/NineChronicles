using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Assets.SimpleLocalization;
using AsyncIO;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Store;
using Libplanet.Tx;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Nekoyume.Action;
using Nekoyume.Game.Item;
using Nekoyume.Helper;
using Nekoyume.Serilog;
using Nekoyume.State;
using Nekoyume.UI;
using NetMQ;
using Serilog;
using UniRx;
using UnityEngine;

namespace Nekoyume.BlockChain
{
    /// <summary>
    /// 블록체인 노드 관련 로직을 처리
    /// </summary>
    public class Agent : MonoBehaviour, IDisposable
    {
        private const string PlayerPrefsKeyOfAgentPrivateKey = "private_key_agent";
#if UNITY_EDITOR
        private const string AgentStoreDirName = "planetarium_dev";
#else
        private const string AgentStoreDirName = "planetarium";
#endif
        private const string DefaultIceServer = "turn://0ed3e48007413e7c2e638f13ddd75ad272c6c507e081bd76a75e4b7adc86c9af:0apejou+ycZFfwtREeXFKdfLj2gCclKzz5ZJ49Cmy6I=@planetarium-turn.koreacentral.cloudapp.azure.com:3478/";

        private static readonly string CommandLineOptionsJsonPath =
            Path.Combine(Application.streamingAssetsPath, "clo.json");
        private const int MaxSeed = 3;

        private string _defaultStoragePath;

        public ReactiveProperty<long> blockIndex = new ReactiveProperty<long>();

        private static IEnumerator _miner;
        private static IEnumerator _txProcessor;
        private static IEnumerator _swarmRunner;
        private static IEnumerator _autoPlayer;
        private static IEnumerator _logger;
        private const float TxProcessInterval = 3.0f;
        private const int SwarmDialTimeout = 5000;
        private const int SwarmLinger = 1 * 1000;
        private const string QueuedActionsFileName = "queued_actions.dat";

        private static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan SleepInterval = TimeSpan.FromSeconds(3);

        private readonly ConcurrentQueue<PolymorphicAction<ActionBase>> _queuedActions =
            new ConcurrentQueue<PolymorphicAction<ActionBase>>();

        protected BlockChain<PolymorphicAction<ActionBase>> blocks;
        private Swarm<PolymorphicAction<ActionBase>> _swarm;
        protected DefaultStore store;
        private ImmutableList<Peer> _seedPeers;
        private IImmutableSet<Address> _trustedPeers;

        private static CancellationTokenSource _cancellationTokenSource;

        private string _tipInfo = string.Empty;

        public long BlockIndex => blocks?.Tip?.Index ?? 0;

        public IEnumerable<Transaction<PolymorphicAction<ActionBase>>> StagedTransactions =>
            store.IterateStagedTransactionIds()
                .Select(store.GetTransaction<PolymorphicAction<ActionBase>>);

        protected PrivateKey PrivateKey { get; private set; }
        public Address Address { get; set; }

        public event EventHandler BootstrapStarted;
        public event EventHandler PreloadStarted;
        public event EventHandler<PreloadState> PreloadProcessed;
        public event EventHandler PreloadEnded;
        public event EventHandler<long> TipChanged;
        public static event Action<Guid> OnEnqueueOwnGameAction;
        public static event Action<bool> OnHasOwnTx;

        private bool SyncSucceed { get; set; }
        public bool BlockDownloadFailed { get; private set; }

        private static TelemetryClient _telemetryClient;

        private const string InstrumentationKey = "953da29a-95f7-4f04-9efe-d48c42a1b53a";

        public bool disposed;

        private void Awake()
        {
            ForceDotNet.Force();
            _defaultStoragePath = Path.Combine(Application.persistentDataPath, AgentStoreDirName);
        }

        public void Initialize(Action<bool> callback)
        {
            if (disposed)
            {
                Debug.Log("Agent Exist");
                return;
            }

            disposed = false;
            StartCoroutine(CoLogin(callback));
        }

        public void Init(
            PrivateKey privateKey,
            string path,
            IEnumerable<Peer> peers,
            IEnumerable<IceServer> iceServers,
            string host,
            int? port,
            bool consoleSink,
            bool development)
        {
            InitializeLogger(consoleSink, development);

            Debug.Log(path);
            var policy = GetPolicy();
            PrivateKey = privateKey;
            Address = privateKey.PublicKey.ToAddress();
            store = new DefaultStore(path, flush: false);
            blocks = new BlockChain<PolymorphicAction<ActionBase>>(policy, store);
#if BLOCK_LOG_USE
            FileHelper.WriteAllText("Block.log", "");
#endif

            _swarm = new Swarm<PolymorphicAction<ActionBase>>(
                blocks,
                privateKey,
                appProtocolVersion: 1,
                host: host,
                listenPort: port,
                iceServers: iceServers,
                differentVersionPeerEncountered: DifferentAppProtocolVersionPeerEncountered);

            if (!consoleSink) InitializeTelemetryClient(_swarm.Address);

            ImmutableList<Peer> peerList = peers
                .Where(peer => peer.PublicKey != privateKey.PublicKey)
                .ToImmutableList();
            _seedPeers = (peerList.Count > MaxSeed ? peerList.Sample(MaxSeed) : peerList)
                .ToImmutableList();
            // Init SyncSucceed
            SyncSucceed = true;

            // FIXME: Trusted peers should be configurable
            _trustedPeers = _seedPeers.Select(peer => peer.Address).ToImmutableHashSet();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        private void InitAgent(Action<bool> callback, PrivateKey privateKey)
        {
            var options = GetOptions(CommandLineOptionsJsonPath);
            var peers = options.Peers.Select(LoadPeer);
            var iceServers = options.IceServers.Select(LoadIceServer);

            if (!iceServers.Any())
            {
                iceServers = new[] { LoadIceServer(DefaultIceServer) };
            }
            var host = GetHost(options);
            var port = options.Port;
            var consoleSink = options.ConsoleSink;
            var storagePath = options.StoragePath ?? _defaultStoragePath;
            var development = options.Development;
            Init(
                privateKey,
                storagePath,
                peers,
                iceServers,
                host,
                port,
                consoleSink,
                development
            );

            // 별도 쓰레드에서는 GameObject.GetComponent<T> 를 사용할 수 없기때문에 미리 선언.
            var loadingScreen = Widget.Find<PreloadingScreen>();
            BootstrapStarted += (_, state) =>
                loadingScreen.Message = LocalizationManager.Localize("UI_LOADING_BOOTSTRAP_START");
            PreloadProcessed += (_, state) =>
            {
                if (loadingScreen)
                {
                    string text;
                    string format;

                    switch (state)
                    {
                        case BlockDownloadState blockDownloadState:
                            format = LocalizationManager.Localize("UI_LOADING_BLOCK_DOWNLOAD");
                            text = string.Format(format, blockDownloadState.ReceivedBlockCount,
                                blockDownloadState.TotalBlockCount);
                            break;

                        case StateReferenceDownloadState stateReferenceDownloadState:
                            format = LocalizationManager.Localize("UI_LOADING_STATE_REFERENCE_DOWNLOAD");
                            text = string.Format(format, stateReferenceDownloadState.ReceivedStateReferenceCount,
                                stateReferenceDownloadState.TotalStateReferenceCount);
                            break;

                        case BlockStateDownloadState blockStateDownloadState:
                            text =
                                $"{blockStateDownloadState.ReceivedBlockStateCount} / {blockStateDownloadState.TotalBlockStateCount}";
                            break;

                        case ActionExecutionState actionExecutionState:
                            text =
                                $"{actionExecutionState.ExecutedBlockCount} / {actionExecutionState.TotalBlockCount}";
                            break;

                        default:
                            throw new Exception("Unknown state was reported during preload.");
                    }

                    loadingScreen.Message = $"{text}  ({state.CurrentPhase} / {PreloadState.TotalPhase})";
                }
            };
            PreloadEnded += (_, __) =>
            {
                // 에이전트의 준비단계가 끝나면 에이전트의 상태를 한 번 동기화 한다.
                States.Instance.AgentState.Value =
                    GetState(Address) is Bencodex.Types.Dictionary agentDict
                        ? new AgentState(agentDict)
                        : new AgentState(Address);
                // 에이전트에 포함된 모든 아바타의 상태를 한 번씩 동기화 한다.
                foreach (var pair in States.Instance.AgentState.Value.avatarAddresses)
                {
                    var avatarState = new AvatarState(
                        (Bencodex.Types.Dictionary) GetState(pair.Value)
                    );
                    States.Instance.AvatarStates.Add(pair.Key, avatarState);
                }

                // 랭킹의 상태를 한 번 동기화 한다.
                States.Instance.RankingState.Value =
                    GetState(RankingState.Address) is Bencodex.Types.Dictionary rankingDict
                        ? new RankingState(rankingDict)
                        : new RankingState();
                // 상점의 상태를 한 번 동기화 한다.
                States.Instance.ShopState.Value =
                    GetState(ShopState.Address) is Bencodex.Types.Dictionary shopDict
                        ? new ShopState(shopDict)
                        : new ShopState();
                // 그리고 모든 액션에 대한 랜더를 핸들링하기 시작한다.
                ActionRenderHandler.Instance.Start();
                // 그리고 마이닝을 시작한다.
                StartNullableCoroutine(_miner);
                StartCoroutine(CoCheckBlockTip());

                StartNullableCoroutine(_autoPlayer);
                callback(SyncSucceed);
                LoadQueuedActions();
                TipChanged += (___, index) => { blockIndex.Value = index; };
            };
            _miner = options.NoMiner ? null : CoMiner();
            _autoPlayer = options.AutoPlay ? CoAutoPlayer() : null;

#if DEBUG
            if (development)
            {
                _logger = CoLogger();
            }
#endif

            StartSystemCoroutines();
            StartCoroutine(CoCheckStagedTxs());
        }

        private IEnumerator CoCheckBlockTip()
        {
            while (true)
            {
                var current = BlockIndex;
                yield return new WaitForSeconds(180f);
                if (BlockIndex == current)
                {
                    Widget.Find<BlockFailPopup>().Show(current);
                    break;
                }
            }
        }

        public static CommandLineOptions GetOptions(string jsonPath)
        {
            if (File.Exists(jsonPath))
            {
                return JsonUtility.FromJson<CommandLineOptions>(
                    File.ReadAllText(jsonPath)
                );
            }
            else
            {
                return CommnadLineParser.GetCommandLineOptions() ?? new CommandLineOptions();
            }
        }

        private static string GetHost(CommandLineOptions options)
        {
            return string.IsNullOrEmpty(options.host) ? null : options.Host;
        }

        private static BoundPeer LoadPeer(string peerInfo)
        {
            var tokens = peerInfo.Split(',');
            var pubKey = new PublicKey(ByteUtil.ParseHex(tokens[0]));
            var host = tokens[1];
            var port = int.Parse(tokens[2]);

            return new BoundPeer(pubKey, new DnsEndPoint(host, port), 0);
        }

        private static IceServer LoadIceServer(string iceServerInfo)
        {
            var uri = new Uri(iceServerInfo);
            string[] userInfo = uri.UserInfo.Split(':');

            return new IceServer(new[] {uri}, userInfo[0], userInfo[1]);
        }

        #region Mono

        protected void OnDestroy()
        {
            ActionRenderHandler.Instance.Stop();
            Dispose();
        }

        #endregion

        private void StartSystemCoroutines()
        {
            _txProcessor = CoTxProcessor();
            _swarmRunner = CoSwarmRunner();

            StartNullableCoroutine(_txProcessor);
            StartNullableCoroutine(_swarmRunner);
            StartNullableCoroutine(_logger);
        }

        private void StartNullableCoroutine(IEnumerator routine)
        {
            if (!(routine is null))
            {
                StartCoroutine(routine);
            }
        }

        private static bool WantsToQuit()
        {
            NetMQConfig.Cleanup(false);
            return true;
        }

        [RuntimeInitializeOnLoadMethod]
        private static void RunOnStart()
        {
            Application.wantsToQuit += WantsToQuit;
        }

        private class DebugPolicy : IBlockPolicy<PolymorphicAction<ActionBase>>
        {
            public IAction BlockAction { get; } = new RewardGold {gold = 1};

            public InvalidBlockException ValidateNextBlock(
                BlockChain<PolymorphicAction<ActionBase>> blocks,
                Block<PolymorphicAction<ActionBase>> nextBlock
            )
            {
                return null;
            }

            public long GetNextBlockDifficulty(BlockChain<PolymorphicAction<ActionBase>> blocks)
            {
                Thread.Sleep(SleepInterval);
                return blocks.Tip is null ? 0 : 1;
            }
        }

        private static void InitializeTelemetryClient(Address address)
        {
            _telemetryClient.Context.User.AuthenticatedUserId = address.ToHex();
            _telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
        }

        private static void InitializeLogger(bool consoleSink, bool development)
        {
            LoggerConfiguration loggerConfiguration;
            if (development)
            {
                loggerConfiguration = new LoggerConfiguration().MinimumLevel.Verbose();
            }
            else
            {
                loggerConfiguration = new LoggerConfiguration().MinimumLevel.Information();
            }

            if (consoleSink)
            {
                loggerConfiguration = loggerConfiguration
                    .WriteTo.Sink(new UnityDebugSink());
            }
            else
            {
                _telemetryClient =
                    new TelemetryClient(new TelemetryConfiguration(InstrumentationKey));

                loggerConfiguration = loggerConfiguration
                    .WriteTo.ApplicationInsights(_telemetryClient, TelemetryConverter.Traces);
            }

            Log.Logger = loggerConfiguration.CreateLogger();
        }

        private void DifferentAppProtocolVersionPeerEncountered(object sender, DifferentProtocolVersionEventArgs e)
        {
            Debug.LogWarningFormat("Different Version Encountered Expected: {0} Actual : {1}",
                e.ExpectedVersion, e.ActualVersion);
            SyncSucceed = false;
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            // `_swarm`의 내부 큐가 비워진 다음 완전히 종료할 때까지 더 기다립니다.
            Task.Run(async () => { await _swarm?.StopAsync(TimeSpan.FromMilliseconds(SwarmLinger)); })
                .ContinueWith(_ =>
                {
                    store?.Dispose();
                    _swarm?.Dispose();
                })
                .Wait(SwarmLinger + 1 * 1000);

            States.Dispose();
            SaveQueuedActions();
            disposed = true;
        }

        private IEnumerator CoLogger()
        {
            while (true)
            {
                Cheat.Display("Logs", _tipInfo);
                Cheat.Display("Peers", _swarm?.TraceTable());
                var log = $"Staged Transactions : {store.IterateStagedTransactionIds().Count()}\n";
                var count = 1;
                foreach (var id in store.IterateStagedTransactionIds())
                {
                    var tx = store.GetTransaction<PolymorphicAction<ActionBase>>(id);
                    log += $"[{count++}] Id : {tx.Id}\n";
                    log += $"-Signer : {tx.Signer.ToString()}\n";
                    log += $"-Nonce : {tx.Nonce}\n";
                    log += $"-Timestamp : {tx.Timestamp}\n";
                    log += $"-Actions\n";
                    log = tx.Actions.Aggregate(log, (current, action) => current + $" -{action.InnerAction}\n");
                }

                Cheat.Display("StagedTxs", log);
                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator CoSwarmRunner()
        {
            BootstrapStarted?.Invoke(this, null);
            var bootstrapTask = Task.Run(async () =>
            {
                try
                {
                    await _swarm.BootstrapAsync(
                        seedPeers: _seedPeers,
                        pingSeedTimeout: 5000,
                        findPeerTimeout: 5000,
                        cancellationToken: _cancellationTokenSource.Token
                    );
                }
                catch (SwarmException e)
                {
                    Debug.LogFormat("Bootstrap failed. {0}", e.Message);
                    throw;
                }
                catch (Exception e)
                {
                    Debug.LogFormat("Exception occurred during bootstrap {0}", e);
                    throw;
                }
            });
            yield return new WaitUntil(() => bootstrapTask.IsCompleted);
#if !UNITY_EDITOR
            if (!Application.isBatchMode && (bootstrapTask.IsFaulted || bootstrapTask.IsCanceled))
            {
                var errorMsg = string.Format(LocalizationManager.Localize("UI_ERROR_FORMAT"),
                    LocalizationManager.Localize("BOOTSTRAP_FAIL"));

                Widget.Find<SystemPopup>().Show(
                    LocalizationManager.Localize("UI_ERROR"),
                    errorMsg,
                    LocalizationManager.Localize("UI_QUIT"),
                    false
                );
                yield break;
            }
#endif
            PreloadStarted?.Invoke(this, null);
            Debug.Log("PreloadingStarted event was invoked");

            var started = DateTimeOffset.UtcNow;
            var existingBlocks = blocks?.Tip?.Index ?? 0;
            Debug.Log("Preloading starts");

            // _swarm.PreloadAsync() 에서 대기가 발생하기 때문에
            // 이를 다른 스레드에서 실행하여 우회하기 위해 Task로 감쌉니다.
            var swarmPreloadTask = Task.Run(async () =>
            {
                await _swarm.PreloadAsync(
                    TimeSpan.FromMilliseconds(SwarmDialTimeout),
                    new Progress<PreloadState>(state =>
                        PreloadProcessed?.Invoke(this, state)
                    ),
                    trustedStateValidators: _trustedPeers,
                    cancellationToken: _cancellationTokenSource.Token,
                    blockDownloadFailed: PreloadBLockDownloadFailed
                );
            });

            yield return new WaitUntil(() => swarmPreloadTask.IsCompleted);
            var ended = DateTimeOffset.UtcNow;

            if (swarmPreloadTask.Exception is Exception exc)
            {
                Debug.LogErrorFormat(
                    "Preloading terminated with an exception: {0}",
                    exc
                );
                throw exc;
            }

            var index = blocks?.Tip?.Index ?? 0;
            Debug.LogFormat(
                "Preloading finished; elapsed time: {0}; blocks: {1}",
                ended - started,
                index - existingBlocks
            );


            PreloadEnded?.Invoke(this, null);

            var swarmStartTask = Task.Run(async () =>
            {
                try
                {
                    await _swarm.StartAsync(preloadBlockDownloadFailed: PreloadBLockDownloadFailed);
                }
                catch (TaskCanceledException)
                {
                }
                // Avoid TerminatingException in test.
                catch (TerminatingException)
                {
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat(
                        "Swarm terminated with an exception: {0}",
                        e
                    );
                    throw;
                }
            });

            Task.Run(async () =>
            {
                await _swarm.WaitForRunningAsync();
                blocks.TipChanged += TipChangedHandler;

                Debug.LogFormat(
                    "The address of this node: {0},{1},{2}",
                    ByteUtil.Hex(PrivateKey.PublicKey.Format(true)),
                    _swarm.EndPoint.Host,
                    _swarm.EndPoint.Port
                );
            });

            yield return new WaitUntil(() => swarmStartTask.IsCompleted);
        }

        private void TipChangedHandler(
            object target,
            BlockChain<PolymorphicAction<ActionBase>>.TipChangedEventArgs args)
        {
            _tipInfo = "Tip Information\n";
            _tipInfo += $" -Miner           : {blocks.Tip.Miner?.ToString()}\n";
            _tipInfo += $" -TimeStamp  : {DateTimeOffset.Now}\n";
            _tipInfo += $" -PrevBlock    : [{args.PreviousIndex}] {args.PreviousHash}\n";
            _tipInfo += $" -LatestBlock : [{args.Index}] {args.Hash}";
            TipChanged?.Invoke(null, args.Index);
        }

        private IEnumerator CoTxProcessor()
        {
            while (true)
            {
                yield return new WaitForSeconds(TxProcessInterval);

                var actions = new List<PolymorphicAction<ActionBase>>();

                Debug.LogFormat("Try Dequeue Actions. Total Count: {0}", _queuedActions.Count);
                while (_queuedActions.TryDequeue(out PolymorphicAction<ActionBase> action))
                {
                    actions.Add(action);
                    Debug.LogFormat("Remain Queued Actions Count: {0}", _queuedActions.Count);
                }

                Debug.LogFormat("Finish Dequeue Actions.");

                if (actions.Any())
                {
                    var task = Task.Run(() => MakeTransaction(actions));
                    yield return new WaitUntil(() => task.IsCompleted);
                }
            }
        }

        private IEnumerator CoAutoPlayer()
        {
            var avatarIndex = 0;
            var avatarAddress = AvatarManager.CreateAvatarAddress();
            var dummyName = Address.ToHex().Substring(0, 8);

            yield return ActionManager.instance
                .CreateAvatar(avatarAddress, avatarIndex, dummyName)
                .ToYieldInstruction();
            Debug.LogFormat("Autoplay[{0}, {1}]: CreateAvatar", avatarAddress.ToHex(), dummyName);

            AvatarManager.SetIndex(avatarIndex);
            var waitForSeconds = new WaitForSeconds(TxProcessInterval);

            while (true)
            {
                yield return waitForSeconds;

                yield return ActionManager.instance.HackAndSlash(
                    new List<Equipment>(), new List<Consumable>(), 1, 1).ToYieldInstruction();
                Debug.LogFormat("Autoplay[{0}, {1}]: HackAndSlash", avatarAddress.ToHex(), dummyName);
            }
        }

        private IEnumerator CoMiner()
        {
            while (true)
            {
                var txs = new HashSet<Transaction<PolymorphicAction<ActionBase>>>();

                var timeStamp = DateTimeOffset.UtcNow;
                var prevTimeStamp = blocks?.Tip?.Timestamp;
                //FIXME 년도가 바뀌면 깨지는 계산 방식. 테스트 끝나면 변경해야함
                // 하루 한번 보상을 제공
                if (prevTimeStamp is DateTimeOffset t && timeStamp.DayOfYear - t.DayOfYear == 1)
                {
                    var rankingRewardTx = RankingReward();
                    txs.Add(rankingRewardTx);
                }

                var task = Task.Run(async () =>
                {
                    var block = await blocks.MineBlock(Address);
                    if (_swarm.Running)
                    {
                        _swarm.BroadcastBlocks(new[] {block});
                    }

                    return block;
                });
                yield return new WaitUntil(() => task.IsCompleted);

                if (!task.IsCanceled && !task.IsFaulted)
                {
                    var block = task.Result;
                    Debug.Log($"created block index: {block.Index}, difficulty: {block.Difficulty}");
#if BLOCK_LOG_USE
                    FileHelper.AppendAllText("Block.log", task.Result.ToVerboseString());
#endif
                }
                else if (task.Exception?.InnerExceptions.OfType<OperationCanceledException>().Count() != 0)
                {
                    Debug.Log("Mining was canceled due to change of tip.");
                }
                else
                {
                    var invalidTxs = txs;
                    var retryActions = new HashSet<IImmutableList<PolymorphicAction<ActionBase>>>();

                    if (task.IsFaulted)
                    {
                        foreach (var ex in task.Exception.InnerExceptions)
                        {
                            if (ex is InvalidTxNonceException invalidTxNonceException)
                            {
                                var invalidNonceTx = blocks.GetTransaction(invalidTxNonceException.TxId);

                                if (invalidNonceTx.Signer == Address)
                                {
                                    Debug.Log($"Tx[{invalidTxNonceException.TxId}] nonce is invalid. Retry it.");
                                    retryActions.Add(invalidNonceTx.Actions);
                                }
                            }

                            if (ex is InvalidTxException invalidTxException)
                            {
                                Debug.Log($"Tx[{invalidTxException.TxId}] is invalid. mark to unstage.");
                                invalidTxs.Add(blocks.GetTransaction(invalidTxException.TxId));
                            }
                            else if (ex is UnexpectedlyTerminatedActionException actionException
                                     && actionException.TxId is TxId txId)
                            {
                                Debug.Log($"Tx[{actionException.TxId}]'s action is invalid. mark to unstage. {ex}");
                                invalidTxs.Add(blocks.GetTransaction(txId));
                            }

                            Debug.LogException(ex);
                        }
                    }

                    blocks.UnstageTransactions(invalidTxs);

                    foreach (var retryAction in retryActions)
                    {
                        MakeTransaction(retryAction);
                    }
                }
            }
        }

        public void EnqueueAction(GameAction gameAction)
        {
            Debug.LogFormat("Enqueue GameAction: {0} Id: {1}", gameAction, gameAction.Id);
            _queuedActions.Enqueue(gameAction);
            OnEnqueueOwnGameAction?.Invoke(gameAction.Id);
        }

        public IValue GetState(Address address)
        {
            return blocks.GetState(address);
        }

        private IBlockPolicy<PolymorphicAction<ActionBase>> GetPolicy()
        {
# if UNITY_EDITOR
            return new DebugPolicy();
# else
            return new BlockPolicy<PolymorphicAction<ActionBase>>(
                new RewardGold { gold = 1 },
                BlockInterval,
                100000,
                2048
            );
#endif
        }

        private Transaction<PolymorphicAction<ActionBase>> RankingReward()
        {
            var actions = new List<PolymorphicAction<ActionBase>>
            {
                new RankingReward
                {
                    gold1 = 10,
                    gold2 = 5,
                    gold3 = 3,
                    agentAddresses = States.Instance.RankingState.Value.GetAgentAddresses(3, null),
                }
            };
            return MakeTransaction(actions);
        }

        public void AppendBlock(Block<PolymorphicAction<ActionBase>> block)
        {
            blocks.Append(block);
        }

        private Transaction<PolymorphicAction<ActionBase>> MakeTransaction(
            IEnumerable<PolymorphicAction<ActionBase>> actions)
        {
            var polymorphicActions = actions.ToArray();
            Debug.LogFormat("Make Transaction with Actions: `{0}`",
                string.Join(",", polymorphicActions.Select(i => i.InnerAction)));
            return blocks.MakeTransaction(PrivateKey, polymorphicActions);
        }

        private void LoadQueuedActions()
        {
            var path = Path.Combine(Application.persistentDataPath, QueuedActionsFileName);
            if (File.Exists(path))
            {
                var actionsListBytes = File.ReadAllBytes(path);
                if (actionsListBytes.Any())
                {
                    var actionsList = ByteSerializer.Deserialize<List<GameAction>>(actionsListBytes);
                    foreach (var action in actionsList)
                    {
                        EnqueueAction(action);
                    }

                    Debug.Log($"Load queued actions: {_queuedActions.Count}");
                    File.Delete(path);
                }
            }
        }

        private void SaveQueuedActions()
        {
            if (_queuedActions.Any())
            {
                List<GameAction> actionsList;

                var path = Path.Combine(Application.persistentDataPath, QueuedActionsFileName);
                if (!File.Exists(path))
                {
                    Debug.Log("Create new queuedActions list.");
                    actionsList = new List<GameAction>();
                }
                else
                {
                    actionsList =
                        ByteSerializer.Deserialize<List<GameAction>>(File.ReadAllBytes(path));
                    Debug.Log($"Load queuedActions list. : {actionsList.Count}");
                }

                Debug.LogWarning($"Save QueuedActions : {_queuedActions.Count}");
                while (_queuedActions.TryDequeue(out var action))
                    actionsList.Add((GameAction) action.InnerAction);

                File.WriteAllBytes(path, ByteSerializer.Serialize(actionsList));
            }
        }

        private IEnumerator CoLogin(Action<bool> callback)
        {
            var options = GetOptions(CommandLineOptionsJsonPath);
            var loginPopup = Widget.Find<LoginPopup>();

            if (Application.isBatchMode)
            {
                loginPopup.Show(options.KeyStorePath, options.PrivateKey);
            }
            else
            {
                var title = Widget.Find<Title>();
                title.Show(options.keyStorePath, options.privateKey);
                yield return new WaitUntil(() => loginPopup.Login);
                title.Close();
            }
            InitAgent(callback, loginPopup.GetPrivateKey());
        }

        private IEnumerator CoCheckStagedTxs()
        {
            var hasOwnTx = false;
            while (true)
            {
                var txs = store.IterateStagedTransactionIds()
                    .Select(id => store.GetTransaction<PolymorphicAction<ActionBase>>(id))
                    .Where(tx => tx.Signer.Equals(Address))
                    .ToList();

                if (hasOwnTx)
                {
                    if (txs.Count == 0)
                    {
                        hasOwnTx = false;
                        OnHasOwnTx?.Invoke(false);
                    }
                }
                else
                {
                    if (txs.Count > 0)
                    {
                        hasOwnTx = true;
                        OnHasOwnTx?.Invoke(true);
                    }
                }

                yield return new WaitForSeconds(.3f);
            }
        }

        private void PreloadBLockDownloadFailed(object sender, PreloadBlockDownloadFailEventArgs e)
        {
            SyncSucceed = false;
            BlockDownloadFailed = true;
        }
    }
}
