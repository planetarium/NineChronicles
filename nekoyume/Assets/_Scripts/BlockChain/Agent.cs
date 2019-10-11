using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AsyncIO;
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

#if NEKOALPHA_NOMINER
        private static readonly string CommandLineOptionsJsonPath =
 Path.Combine(Application.streamingAssetsPath, "clo_nekoalpha_nominer.json");
#else
        private static readonly string CommandLineOptionsJsonPath =
            Path.Combine(Application.streamingAssetsPath, "clo.json");
#endif
        private const string PeersFileName = "peers.dat";
        private const string IceServersFileName = "ice_servers.dat";

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
        protected LiteDBStore store;
        private ImmutableList<Peer> _seedPeers;
        private IImmutableSet<Address> _trustedPeers;

        private static CancellationTokenSource _cancellationTokenSource;

        private string _tipInfo = string.Empty;

        public IDictionary<TxId, Transaction<PolymorphicAction<ActionBase>>> Transactions => blocks.Transactions;
        public long BlockIndex => blocks?.Tip?.Index ?? 0;

        protected PrivateKey PrivateKey { get; private set; }
        private Address Address { get; set; }

        public event EventHandler BootstrapStarted;
        public event EventHandler PreloadStarted;
        public event EventHandler<PreloadState> PreloadProcessed;
        public event EventHandler PreloadEnded;
        public event EventHandler<long> TipChanged;

        private bool SyncSucceed { get; set; }

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
            InitAgent(callback);
        }

        public void Init(PrivateKey privateKey, string path, IEnumerable<Peer> peers,
            IEnumerable<IceServer> iceServers, string host, int? port, bool consoleSink)
        {
            Debug.Log(path);
            var policy = GetPolicy();
            PrivateKey = privateKey;
            Address = privateKey.PublicKey.ToAddress();
            store = new LiteDBStore($"{path}.ldb", flush: false);
            blocks = new BlockChain<PolymorphicAction<ActionBase>>(policy, store);
#if BLOCK_LOG_USE
            FileHelper.WriteAllText("Block.log", "");
#endif
            InitializeLogger(consoleSink);

            _swarm = new Swarm<PolymorphicAction<ActionBase>>(
                blocks,
                privateKey,
                appProtocolVersion: 1,
                millisecondsDialTimeout: SwarmDialTimeout,
                millisecondsLinger: SwarmLinger,
                host: host,
                listenPort: port,
                iceServers: iceServers,
                differentVersionPeerEncountered: DifferentAppProtocolVersionPeerEncountered);

            if (!consoleSink) InitializeTelemetryClient(_swarm.Address);

            _seedPeers = peers.Where(peer => peer.PublicKey != privateKey.PublicKey).ToImmutableList();
            // Init SyncSucceed
            SyncSucceed = true;

            // FIXME: Trusted peers should be configurable
            _trustedPeers = _seedPeers.Select(peer => peer.Address).ToImmutableHashSet();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        private void InitAgent(Action<bool> callback)
        {
            var options = GetOptions(CommandLineOptionsJsonPath);
            var privateKey = GetPrivateKey(options);
            var peers = GetPeers(options);
            var iceServers = GetIceServers();
            var host = GetHost(options);
            var port = options.Port;
            var consoleSink = options.ConsoleSink;
            var storagePath = options.StoragePath ?? _defaultStoragePath;
            Init(privateKey, storagePath, peers, iceServers, host, port, consoleSink);

            // 별도 쓰레드에서는 GameObject.GetComponent<T> 를 사용할 수 없기때문에 미리 선언.
            var loadingScreen = Widget.Find<LoadingScreen>();
            BootstrapStarted += (_, state) => { loadingScreen.Message = "네트워크 연결을 수립하고 있습니다..."; };
            PreloadProcessed += (_, state) =>
            {
                if (loadingScreen)
                {
                    string text;

                    switch (state)
                    {
                        case BlockDownloadState blockDownloadState:
                            text = "블록 다운로드 중... " +
                                   $"{blockDownloadState.ReceivedBlockCount} / {blockDownloadState.TotalBlockCount}";
                            break;

                        case StateReferenceDownloadState stateReferenceDownloadState:
                            text = "상태 다운로드 중... " +
                                   $"{stateReferenceDownloadState.ReceivedStateReferenceCount} / {stateReferenceDownloadState.TotalStateReferenceCount}";
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
                States.Instance.agentState.Value = (AgentState) GetState(Address) ??
                                                   new AgentState(Address);
                // 에이전트에 포함된 모든 아바타의 상태를 한 번씩 동기화 한다.
                foreach (var pair in States.Instance.agentState.Value.avatarAddresses)
                {
                    var avatarState = (AvatarState) GetState(pair.Value);
                    States.Instance.avatarStates.Add(pair.Key, avatarState);
                }

                // 랭킹의 상태를 한 번 동기화 한다.
                States.Instance.rankingState.Value =
                    (RankingState) GetState(RankingState.Address) ?? new RankingState();
                // 상점의 상태를 한 번 동기화 한다.
                States.Instance.shopState.Value = (ShopState) GetState(ShopState.Address) ?? new ShopState();
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
            StartSystemCoroutines();
        }

        private IEnumerator CoCheckBlockTip()
        {
            while (true)
            {
                var current = BlockIndex;
                yield return new WaitForSeconds(180f);
                if (BlockIndex == current)
                {
                    Widget.Find<ExitPopup>().Show(current);
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

        private static PrivateKey GetPrivateKey(CommandLineOptions options)
        {
            PrivateKey privateKey;
            var privateKeyHex = options.PrivateKey ?? PlayerPrefs.GetString(PlayerPrefsKeyOfAgentPrivateKey, "");

            if (string.IsNullOrEmpty(privateKeyHex))
            {
                privateKey = new PrivateKey();
                PlayerPrefs.SetString(PlayerPrefsKeyOfAgentPrivateKey, ByteUtil.Hex(privateKey.ByteArray));
            }
            else
            {
                privateKey = new PrivateKey(ByteUtil.ParseHex(privateKeyHex));
            }

            return privateKey;
        }

        private static IEnumerable<Peer> GetPeers(CommandLineOptions options)
        {
            return options.Peers?.Any() ?? false
                ? options.Peers.Select(LoadPeer)
                : LoadConfigLines(PeersFileName).Select(LoadPeer);
        }

        private static IEnumerable<IceServer> GetIceServers()
        {
            return LoadIceServers();
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

        private static IEnumerable<string> LoadConfigLines(string fileName)
        {
            var userPath = Path.Combine(
                Application.persistentDataPath,
                fileName
            );
            string content;

            if (File.Exists(userPath))
            {
                content = File.ReadAllText(userPath);
            }
            else
            {
                var assetName = Path.GetFileNameWithoutExtension(fileName);
                content = Resources.Load<TextAsset>($"Config/{assetName}").text;
            }

            foreach (var line in Regex.Split(content, "\n|\r|\r\n"))
            {
                if (!string.IsNullOrEmpty(line.Trim()))
                {
                    yield return line;
                }
            }
        }

        private static IEnumerable<IceServer> LoadIceServers()
        {
            return LoadConfigLines(IceServersFileName)
                .Select(line => new Uri(line))
                .Select(uri => new {uri, userInfo = uri.UserInfo.Split(':')})
                .Select(@t => new IceServer(new[] {@t.uri}, @t.userInfo[0], @t.userInfo[1]));
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
#if DEBUG
            _logger = CoLogger();
#endif

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

            public InvalidBlockException ValidateNextBlock(IReadOnlyList<Block<PolymorphicAction<ActionBase>>> blocks,
                Block<PolymorphicAction<ActionBase>> nextBlock)
            {
                return null;
            }

            public long GetNextBlockDifficulty(IReadOnlyList<Block<PolymorphicAction<ActionBase>>> blocks)
            {
                Thread.Sleep(SleepInterval);
                return blocks.Any() ? 1 : 0;
            }
        }

        private static void InitializeTelemetryClient(Address address)
        {
            _telemetryClient.Context.User.AuthenticatedUserId = address.ToHex();
            _telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
        }

        private static void InitializeLogger(bool consoleSink)
        {
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Verbose();

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
            Task.Run(async () => await _swarm?.StopAsync()).ContinueWith(_ => { store?.Dispose(); })
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
                        _seedPeers,
                        5000,
                        5000,
                        _cancellationTokenSource.Token);
                }
                catch (SwarmException e)
                {
                    Debug.LogFormat("Bootstrap failed. {0}", e.Message);
                }
                catch (Exception e)
                {
                    Debug.LogFormat("Exception occurred during bootstrap {0}", e);
                }
            });

            yield return new WaitUntil(() => bootstrapTask.IsCompleted);

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
                    new Progress<PreloadState>(state =>
                        PreloadProcessed?.Invoke(this, state)
                    ),
                    trustedStateValidators: _trustedPeers,
                    cancellationToken: _cancellationTokenSource.Token
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
                    await _swarm.StartAsync();
                }
                catch (TaskCanceledException)
                {
                }
                //Avoid TerminatingException in test.
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
                    var task = Task.Run(() => MakeTransaction(actions, true));
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
                    new List<Equipment>(), new List<Consumable>(), 1).ToYieldInstruction();
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
                    _swarm.BroadcastBlocks(new[] {block});
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
                                var invalidNonceTx = blocks.Transactions[invalidTxNonceException.TxId];

                                if (invalidNonceTx.Signer == Address)
                                {
                                    Debug.Log($"Tx[{invalidTxNonceException.TxId}] nonce is invalid. Retry it.");
                                    retryActions.Add(invalidNonceTx.Actions);
                                }
                            }

                            if (ex is InvalidTxException invalidTxException)
                            {
                                Debug.Log($"Tx[{invalidTxException.TxId}] is invalid. mark to unstage.");
                                invalidTxs.Add(blocks.Transactions[invalidTxException.TxId]);
                            }

                            Debug.LogException(ex);
                        }
                    }

                    blocks.UnstageTransactions(invalidTxs);

                    foreach (var retryAction in retryActions)
                    {
                        MakeTransaction(retryAction, true);
                    }
                }
            }
        }

        public void EnqueueAction(GameAction gameAction)
        {
            Debug.LogFormat("Enqueue GameAction: {0} Id: {1}", gameAction, gameAction.Id);
            _queuedActions.Enqueue(gameAction);
        }

        public object GetState(Address address)
        {
            var states = blocks.GetStates(new[] {address});
            states.TryGetValue(address, out var value);
            return value;
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
                    agentAddresses = States.Instance.rankingState.Value.GetAgentAddresses(3, null),
                }
            };
            return MakeTransaction(actions, false);
        }

        public void AppendBlock(Block<PolymorphicAction<ActionBase>> block)
        {
            blocks.Append(block);
        }

        private Transaction<PolymorphicAction<ActionBase>> MakeTransaction(
            IEnumerable<PolymorphicAction<ActionBase>> actions, bool broadcast)
        {
            var polymorphicActions = actions.ToArray();
            Debug.LogFormat("Make Transaction with Actions: `{0}`",
                string.Join(",", polymorphicActions.Select(i => i.InnerAction)));
            return blocks.MakeTransaction(PrivateKey, polymorphicActions, broadcast: broadcast);
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
    }
}
