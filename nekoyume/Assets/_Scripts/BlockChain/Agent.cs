using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Serilog;
using Nekoyume.State;
using Nekoyume.UI;
using NetMQ;
using Serilog;
using UniRx;
using UnityEngine;
using UnityEngine.Assertions;

namespace Nekoyume.BlockChain
{
    /// <summary>
    /// 블록체인 노드 관련 로직을 처리
    /// </summary>
    public class Agent : MonoBehaviour, IDisposable, IAgent
    {
        private const string DefaultIceServer = "turn://0ed3e48007413e7c2e638f13ddd75ad272c6c507e081bd76a75e4b7adc86c9af:0apejou+ycZFfwtREeXFKdfLj2gCclKzz5ZJ49Cmy6I=@turn.planetarium.dev:3478/";

        private const int MaxSeed = 3;

        private static readonly string DefaultStoragePath = StorePath.GetDefaultStoragePath();

        private static readonly string PrevStorageDirectoryPath = Path.Combine(StorePath.GetPrefixPath(), "prev_storage");

        private Subject<long> _blockIndexSubject = new Subject<long>();

        public Subject<long> BlockIndexSubject { get => _blockIndexSubject; }

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
        private static readonly TimeSpan SleepInterval = TimeSpan.FromSeconds(15);

        private readonly ConcurrentQueue<PolymorphicAction<ActionBase>> _queuedActions =
            new ConcurrentQueue<PolymorphicAction<ActionBase>>();

        protected BlockChain<PolymorphicAction<ActionBase>> blocks;
        private Swarm<PolymorphicAction<ActionBase>> _swarm;
        protected DefaultStore store;
        private ImmutableList<Peer> _seedPeers;
        private IImmutableSet<Address> _trustedPeers;
        private ImmutableList<Peer> _peerList;

        private static CancellationTokenSource _cancellationTokenSource;

        private string _tipInfo = string.Empty;
        private CommandLineOptions _options;

        private ConcurrentQueue<(Block<PolymorphicAction<ActionBase>>, DateTimeOffset)> lastTenBlocks;

        public long BlockIndex => blocks?.Tip?.Index ?? 0;

        public IEnumerable<Transaction<PolymorphicAction<ActionBase>>> StagedTransactions =>
            store.IterateStagedTransactionIds()
                .Select(store.GetTransaction<PolymorphicAction<ActionBase>>);

        protected PrivateKey PrivateKey { get; private set; }
        public Address Address { get; set; }

        public event EventHandler BootstrapStarted;
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

        static Agent()
        {
            try
            {
                Libplanet.Crypto.CryptoConfig.CryptoBackend = new Secp256K1CryptoBackend();
            }
            catch(Exception e)
            {
                Debug.Log("Secp256K1CryptoBackend initialize failed. Use default backend.");
                Debug.LogException(e);
            }
        }

        public void Initialize(
            CommandLineOptions options,
            PrivateKey privateKey,
            Action<bool> callback)
        {
            if (disposed)
            {
                Debug.Log("Agent Exist");
                return;
            }
            
            _options = options;
            disposed = false;

            InitAgent(callback, privateKey);
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

#if UNITY_EDITOR
            var genesisBlockPath = BlockHelper.GenesisBlockPathDev;
#else
            var genesisBlockPath = BlockHelper.GenesisBlockPathProd;
#endif
            var genesisBlock = BlockHelper.ImportBlock(genesisBlockPath);
            if (genesisBlock is null)
            {
                Debug.LogError("There is no genesis block.");
            }

            Debug.Log($"Store Path: {path}");
            Debug.Log($"Genesis Block Hash: {genesisBlock.Hash}");

            var policy = BlockPolicy.GetPolicy();
            PrivateKey = privateKey;
            Address = privateKey.PublicKey.ToAddress();
            store = new DefaultStore(path, flush: false);

            try
            {
                blocks = new BlockChain<PolymorphicAction<ActionBase>>(policy, store, genesisBlock);
            }
            catch (InvalidGenesisBlockException)
            {
                Widget.Find<SystemPopup>().Show("UI_RESET_STORE", "UI_RESET_STORE_CONTENT");
            }

#if BLOCK_LOG_USE
            FileHelper.WriteAllText("Block.log", "");
#endif
            lastTenBlocks = new ConcurrentQueue<(Block<PolymorphicAction<ActionBase>>, DateTimeOffset)>();

            _swarm = new Swarm<PolymorphicAction<ActionBase>>(
                blocks,
                privateKey,
                appProtocolVersion: 1,
                host: host,
                listenPort: port,
                iceServers: iceServers,
                differentVersionPeerEncountered: DifferentAppProtocolVersionPeerEncountered);

            if (!consoleSink) InitializeTelemetryClient(_swarm.Address);

            _peerList = peers
                .Where(peer => peer.PublicKey != privateKey.PublicKey)
                .ToImmutableList();
            _seedPeers = (_peerList.Count > MaxSeed ? _peerList.Sample(MaxSeed) : _peerList)
                .ToImmutableList();
            // Init SyncSucceed
            SyncSucceed = true;

            // FIXME: Trusted peers should be configurable
            _trustedPeers = _seedPeers.Select(peer => peer.Address).ToImmutableHashSet();
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
        public void ResetStore()
        {
            var confirm = Widget.Find<Confirm>();
            var storagePath = _options.StoragePath ?? DefaultStoragePath;
            var prevStoragePath = Path.Combine(PrevStorageDirectoryPath, $"{storagePath}_{UnityEngine.Random.Range(0, 100)}");
            confirm.CloseCallback = result =>
            {
                if (result == ConfirmResult.No)
                    return;
                Dispose();
                if (Directory.Exists(storagePath))
                {
                    Directory.Move(storagePath, prevStoragePath);
                }
#if UNITY_EDITOR
                UnityEditor.EditorApplication.ExitPlaymode();
#else
                Application.Quit();
#endif
            };
            confirm.Show("UI_CONFIRM_RESET_STORE_TITLE", "UI_CONFIRM_RESET_STORE_CONTENT");
        }

        public void ResetKeyStore()
        {
            var confirm = Widget.Find<Confirm>();
            confirm.CloseCallback = result =>
            {
                if (result == ConfirmResult.No)
                    return;
                Dispose();
                var keyPath = _options.keyStorePath;
                if (Directory.Exists(keyPath))
                {
                    Directory.Delete(keyPath, true);
                }

#if UNITY_EDITOR
                UnityEditor.EditorApplication.ExitPlaymode();
#else
                Application.Quit();
#endif
            };
            confirm.Show("UI_CONFIRM_RESET_KEYSTORE_TITLE", "UI_CONFIRM_RESET_KEYSTORE_CONTENT");
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            // `_swarm`의 내부 큐가 비워진 다음 완전히 종료할 때까지 더 기다립니다.
            Task.Run(async () => { await _swarm?.StopAsync(TimeSpan.FromMilliseconds(SwarmLinger)); })
                .ContinueWith(_ =>
                {
                    try
                    {
                        store?.Dispose();
                        _swarm?.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                })
                .Wait(SwarmLinger + 1 * 1000);

            SaveQueuedActions();
            disposed = true;
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

        #region Mono

        private void Awake()
        {
            ForceDotNet.Force();
            if (!Directory.Exists(PrevStorageDirectoryPath))
            {
                Directory.CreateDirectory(PrevStorageDirectoryPath);
            }
            DeletePreviousStore();
        }

        protected void OnDestroy()
        {
            ActionRenderHandler.Instance.Stop();
            Dispose();
        }

        #endregion

        private void InitAgent(Action<bool> callback, PrivateKey privateKey)
        {
            var peers = _options.Peers.Select(LoadPeer);
            var iceServerList = _options.IceServers.Select(LoadIceServer).ToImmutableList();

            if (!iceServerList.Any())
            {
                iceServerList = new[] { LoadIceServer(DefaultIceServer) }.ToImmutableList();
            }

            var iceServers = iceServerList.Sample(1).ToImmutableList();

            var host = GetHost(_options);
            var port = _options.Port;
            var consoleSink = _options.ConsoleSink;
            var storagePath = _options.StoragePath ?? DefaultStoragePath;
            var development = _options.Development;
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

                        case StateDownloadState stateReferenceDownloadState:
                            format = LocalizationManager.Localize("UI_LOADING_STATE_REFERENCE_DOWNLOAD");
                            text = string.Format(format, stateReferenceDownloadState.ReceivedIterationCount,
                                stateReferenceDownloadState.TotalIterationCount);
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
                Assert.IsNotNull(GetState(RankingState.Address));
                Assert.IsNotNull(GetState(ShopState.Address));
                Assert.IsNotNull(GetState(TableSheetsState.Address));

                // 랭킹의 상태를 한 번 동기화 한다.
                States.Instance.SetRankingState(
                    GetState(RankingState.Address) is Bencodex.Types.Dictionary rankingDict
                        ? new RankingState(rankingDict)
                        : new RankingState());

                // 상점의 상태를 한 번 동기화 한다.
                States.Instance.SetShopState(
                    GetState(ShopState.Address) is Bencodex.Types.Dictionary shopDict
                        ? new ShopState(shopDict)
                        : new ShopState());

                if (ArenaHelper.TryGetThisWeekState(BlockIndex, out var weeklyArenaState))
                {
                    States.Instance.SetWeeklyArenaState(weeklyArenaState);
                }
                else
                    throw new FailedToInstantiateStateException<WeeklyArenaState>();

                // 에이전트의 상태를 한 번 동기화 한다.
                States.Instance.SetAgentState(
                    GetState(Address) is Bencodex.Types.Dictionary agentDict
                        ? new AgentState(agentDict)
                        : new AgentState(Address));

                // 그리고 모든 액션에 대한 랜더와 언랜더를 핸들링하기 시작한다.
                ActionRenderHandler.Instance.Start();
                ActionUnrenderHandler.Instance.Start();

                // 그리고 마이닝을 시작한다.
                StartNullableCoroutine(_miner);
                StartCoroutine(CoCheckBlockTip());

                StartNullableCoroutine(_autoPlayer);
                callback(SyncSucceed);
                LoadQueuedActions();
                TipChanged += (___, index) => { BlockIndexSubject.Publish(index); };
            };
            _miner = _options.NoMiner ? null : CoMiner();
            _autoPlayer = _options.AutoPlay ? CoAutoPlayer() : null;

            if (development)
            {
                _logger = CoLogger();
            }

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
            public IAction BlockAction { get; } = new RewardGold {Gold = 1};

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
                loggerConfiguration = new LoggerConfiguration().MinimumLevel.Debug();
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

        private IEnumerator CoLogger()
        {
            Widget.Create<Cheat>(true);
            while (true)
            {
                Cheat.Display("Logs", _tipInfo);
                Cheat.Display("Peers", _swarm?.TraceTable());
                StringBuilder log = new StringBuilder($"Staged Transactions : {store.IterateStagedTransactionIds().Count()}\n");
                var count = 1;
                foreach (var id in store.IterateStagedTransactionIds())
                {
                    var tx = store.GetTransaction<PolymorphicAction<ActionBase>>(id);
                    log.Append($"[{count++}] Id : {tx.Id}\n");
                    log.Append($"-Signer : {tx.Signer.ToString()}\n");
                    log.Append($"-Nonce : {tx.Nonce}\n");
                    log.Append($"-Timestamp : {tx.Timestamp}\n");
                    log.Append($"-Actions\n");
                    log = tx.Actions.Aggregate(log, (current, action) => current.Append($" -{action.InnerAction}\n"));
                }

                Cheat.Display("StagedTxs", log.ToString());
                
                log = new StringBuilder($"Last 10 tips :\n");
                foreach(var (block, appendedTime) in lastTenBlocks.ToArray().Reverse())
                {
                    log.Append($"[{block.Index}] {block.Hash}\n");
                    log.Append($" -Miner : {block.Miner?.ToString()}\n");
                    log.Append($" -Created at : {block.Timestamp}\n");
                    log.Append($" -Appended at : {appendedTime}\n");
                }
                Cheat.Display("Blocks", log.ToString());
                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator CoSwarmRunner()
        {
            BootstrapStarted?.Invoke(this, null);
            if (_peerList.Any())
            {
                var bootstrapTask = Task.Run(async () =>
                {
                    try
                    {
                        await _swarm.BootstrapAsync(
                            seedPeers: _seedPeers,
                            pingSeedTimeout: 5000,
                            findPeerTimeout: 5000,
                            depth: 1,
                            cancellationToken: _cancellationTokenSource.Token
                        );
                    }
                    catch (SwarmException e)
                    {
                        Debug.LogFormat("Bootstrap failed. {0}", e.Message);
                        throw;
                    }
                    catch (TimeoutException)
                    {
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
            }
            PreloadEnded?.Invoke(this, null);

            var swarmStartTask = Task.Run(async () =>
            {
                try
                {
                    await _swarm.StartAsync(millisecondsBroadcastTxInterval: 15000);
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
                Debug.LogFormat("Address: {0}, PublicKey: {1}",
                    PrivateKey.PublicKey.ToAddress(),
                    ByteUtil.Hex(PrivateKey.PublicKey.Format(true)));
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
            while (lastTenBlocks.Count >= 10)
            {
                lastTenBlocks.TryDequeue(out _);
            }
            lastTenBlocks.Enqueue((blocks.Tip, DateTimeOffset.UtcNow));
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
            var avatarAddress = AvatarState.CreateAvatarAddress();
            var dummyName = Address.ToHex().Substring(0, 8);

            yield return ActionManager.instance
                .CreateAvatar(avatarAddress, avatarIndex, dummyName)
                .ToYieldInstruction();
            Debug.LogFormat("Autoplay[{0}, {1}]: CreateAvatar", avatarAddress.ToHex(), dummyName);

            States.Instance.SelectAvatar(avatarIndex);
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
            var miner = new Miner(blocks, _swarm, PrivateKey);
            while (true)
            {
                var task = Task.Run(async() => await miner.MineBlock());
                yield return new WaitUntil(() => task.IsCompleted);
            }
        }

        private IBlockPolicy<PolymorphicAction<ActionBase>> GetPolicy()
        {
# if UNITY_EDITOR
            return new DebugPolicy();
# else
            return new BlockPolicy<PolymorphicAction<ActionBase>>(
                new RewardGold { Gold = 1 },
                BlockInterval,
                100000,
                2048
            );
#endif
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
            Transaction<PolymorphicAction<ActionBase>> tx = blocks.MakeTransaction(PrivateKey, polymorphicActions);
            if (_swarm.Running)
            {
                _swarm.BroadcastTxs(new[] { tx });
            }

            return tx;
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

        private static void DeletePreviousStore()
        {
            var dirs = Directory.GetDirectories(PrevStorageDirectoryPath).ToList();
            foreach (var dir in dirs)
            {
                Task.Run(() => { Directory.Delete(dir, true); });
            }
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
