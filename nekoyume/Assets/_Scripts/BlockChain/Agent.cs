using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncIO;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c.Renderer;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.RocksDBStore;
using Libplanet.Store;
using Libplanet.Store.Trie;
using Libplanet.Tx;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Nekoyume.Action;
using Nekoyume.BlockChain.Policy;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Serilog;
using Nekoyume.State;
using Nekoyume.UI;
using NetMQ;
using Serilog;
using Serilog.Events;
using UnityEngine;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;
using NCTx = Libplanet.Tx.Transaction<Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>>;

namespace Nekoyume.BlockChain
{
    using UniRx;

    /// <summary>
    /// 블록체인 노드 관련 로직을 처리
    /// </summary>
    public class Agent : MonoBehaviour, IDisposable, IAgent
    {
        private const string DefaultIceServer = "turn://0ed3e48007413e7c2e638f13ddd75ad272c6c507e081bd76a75e4b7adc86c9af:0apejou+ycZFfwtREeXFKdfLj2gCclKzz5ZJ49Cmy6I=@turn-us.planetarium.dev:3478/";

        private const int MaxSeed = 3;

        public static readonly string DefaultStoragePath = StorePath.GetDefaultStoragePath();

        public Subject<long> BlockIndexSubject { get; } = new Subject<long>();
        public Subject<BlockHash> BlockTipHashSubject { get; } = new Subject<BlockHash>();

        private static IEnumerator _miner;
        private static IEnumerator _txProcessor;
        private static IEnumerator _swarmRunner;
        private static IEnumerator _autoPlayer;
        private static IEnumerator _logger;
        private const float TxProcessInterval = 3.0f;
        private const int SwarmDialTimeout = 5000;
        private const int SwarmLinger = 1 * 1000;
        private const string QueuedActionsFileName = "queued_actions.dat";

        private readonly ConcurrentQueue<NCAction> _queuedActions =
            new ConcurrentQueue<NCAction>();

        private readonly TransactionMap _transactions = new TransactionMap(20);

        protected BlockChain<NCAction> blocks;
        private Swarm<NCAction> _swarm;
        protected BaseStore store;
        private IStagePolicy<NCAction> _stagePolicy;
        private IStateStore _stateStore;
        private ImmutableList<Peer> _seedPeers;
        private ImmutableList<Peer> _peerList;

        private static CancellationTokenSource _cancellationTokenSource;

        private string _tipInfo = string.Empty;

        private ConcurrentQueue<(Block<NCAction>, DateTimeOffset)> lastTenBlocks;

        public long BlockIndex => blocks?.Tip?.Index ?? 0;
        public PrivateKey PrivateKey { get; private set; }
        public Address Address => PrivateKey.PublicKey.ToAddress();

        public BlockPolicySource BlockPolicySource { get; private set; }

        public BlockRenderer BlockRenderer => BlockPolicySource.BlockRenderer;

        public ActionRenderer ActionRenderer => BlockPolicySource.ActionRenderer;
        public int AppProtocolVersion { get; private set; }
        public BlockHash BlockTipHash => blocks.Tip.Hash;

        private readonly Subject<(NCTx tx, List<NCAction> actions)> _onMakeTransactionSubject =
            new Subject<(NCTx tx, List<NCAction> actions)>();

        public IObservable<(NCTx tx, List<NCAction> actions)> OnMakeTransaction => _onMakeTransactionSubject;

        public event EventHandler BootstrapStarted;
        public event EventHandler<PreloadState> PreloadProcessed;
        public event Func<UniTask> PreloadEndedAsync;
        public event EventHandler<long> TipChanged;
        public static event Action<Guid> OnEnqueueOwnGameAction;
        public static event Action<bool> OnHasOwnTx;

        private bool SyncSucceed { get; set; }
        public AppProtocolVersion EncounteredHighestVersion { get; private set; }

        private static TelemetryClient _telemetryClient;

        private const string InstrumentationKey = "953da29a-95f7-4f04-9efe-d48c42a1b53a";

        public bool disposed;

        static Agent()
        {
            try
            {
                Libplanet.Crypto.CryptoConfig.CryptoBackend = new Secp256K1CryptoBackend<SHA256>();
            }
            catch(Exception e)
            {
                Debug.Log("Secp256K1CryptoBackend initialize failed. Use default backend.");
                Debug.LogException(e);
            }
        }

        public IEnumerator Initialize(
            CommandLineOptions options,
            PrivateKey privateKey,
            Action<bool> callback)
        {
            if (disposed)
            {
                Debug.Log("Agent Exist");
                yield break;
            }

            InitAgent(callback, privateKey, options);
        }

        private void Init(
            PrivateKey privateKey,
            string path,
            IEnumerable<Peer> peers,
            IEnumerable<IceServer> iceServers,
            string host,
            int? port,
            bool consoleSink,
            bool development,
            AppProtocolVersion appProtocolVersion,
            IEnumerable<PublicKey> trustedAppProtocolVersionSigners,
            string storageType = null,
            string genesisBlockPath = null)
        {
            InitializeLogger(consoleSink, development);
            BlockPolicySource = new BlockPolicySource(Log.Logger, LogEventLevel.Debug);

            var genesisBlock = BlockManager.ImportBlock(genesisBlockPath ?? BlockManager.GenesisBlockPath);
            if (genesisBlock is null)
            {
                Debug.LogError("There is no genesis block.");
            }

            Debug.Log($"Store Path: {path}");
            Debug.Log($"Genesis Block Hash: {genesisBlock.Hash}");
            Debug.LogFormat(
                "AppProtocolVersion: {0}\nAppProtocolVersion.Token: {1}",
                appProtocolVersion,
                appProtocolVersion.Token
            );

            var policy = BlockPolicySource.GetPolicy();
            _stagePolicy = new VolatileStagePolicy<NCAction>();
            PrivateKey = privateKey;
            store = LoadStore(path, storageType);

            try
            {
                IKeyValueStore stateKeyValueStore = new RocksDBKeyValueStore(Path.Combine(path, "states"));
                _stateStore = new TrieStateStore(stateKeyValueStore);
                blocks = new BlockChain<NCAction>(
                    policy,
                    _stagePolicy,
                    store,
                    _stateStore,
                    genesisBlock,
                    renderers: BlockPolicySource.GetRenderers()
                );
            }
            catch (InvalidGenesisBlockException)
            {
                var popup = Widget.Find<IconAndButtonSystem>();
                popup.Show("UI_RESET_STORE", "UI_RESET_STORE_CONTENT");
                popup.SetCancelCallbackToExit();
            }

#if BLOCK_LOG_USE
            FileHelper.WriteAllText("Block.log", "");
#endif
            lastTenBlocks = new ConcurrentQueue<(Block<NCAction>, DateTimeOffset)>();

            EncounteredHighestVersion = appProtocolVersion;

            _swarm = new Swarm<NCAction>(
                blocks,
                privateKey,
                appProtocolVersion: appProtocolVersion,
                host: host,
                listenPort: port,
                iceServers: iceServers,
                differentAppProtocolVersionEncountered: DifferentAppProtocolVersionEncountered,
                trustedAppProtocolVersionSigners: trustedAppProtocolVersionSigners);

            if (!consoleSink) InitializeTelemetryClient(_swarm.Address);

            _peerList = peers
                .Where(peer => peer.PublicKey != privateKey.PublicKey)
                .ToImmutableList();
            _seedPeers = (_peerList.Count > MaxSeed ? _peerList.Sample(MaxSeed) : _peerList)
                .ToImmutableList();
            // Init SyncSucceed
            SyncSucceed = true;

            _cancellationTokenSource = new CancellationTokenSource();
        }



        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            // `_swarm`의 내부 큐가 비워진 다음 완전히 종료할 때까지 더 기다립니다.
            Task.Run(async () =>
                {
                    await _swarm?.StopAsync(TimeSpan.FromMilliseconds(SwarmLinger));

                    // 프리로드 중일 경우 StopAsync 에서 딜레이가 없을 수 있어서 딜레이를 추가 합니다.
                    // FIXME: Swarm<T>에 프리로딩을 기다리는 API가 생길 때까지만 이렇게 해둡니다.
                    await Task.Delay(SwarmLinger);
                })
                .ContinueWith(_ =>
                {
                    try
                    {
                        store?.Dispose();
                        _swarm?.Dispose();
                        if (_stateStore is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
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

        public Task<IValue> GetStateAsync(Address address)
        {
            return Task.Run(() => blocks.GetState(address));
        }

        public async Task<Dictionary<Address, AvatarState>> GetAvatarStates(IEnumerable<Address> addressList)
        {
            return await Task.Run(async () =>
            {
                var dict = new Dictionary<Address, AvatarState>();
                foreach (var address in addressList)
                {
                    var result = await States.TryGetAvatarStateAsync(address);
                    if (result.exist)
                    {
                        dict[address] = result.avatarState;
                    }
                }

                return dict;
            });
        }

        public async Task<Dictionary<Address, IValue>> GetStateBulk(IEnumerable<Address> addressList)
        {
            return await Task.Run(async () =>
            {
                var dict = new Dictionary<Address, IValue>();
                foreach (var address in addressList)
                {
                    var result = await GetStateAsync(address);
                    dict[address] = result;
                }

                return dict;
            });
        }

        public bool TryGetTxId(Guid actionId, out TxId txId) =>
            _transactions.TryGetValue(actionId, out txId);

        public UniTask<bool> IsTxStagedAsync(TxId txId) =>
            UniTask.FromResult(blocks.GetStagedTransactionIds().Contains(txId));

        public FungibleAssetValue GetBalance(Address address, Currency currency) =>
            blocks.GetBalance(address, currency);

        public Task<FungibleAssetValue> GetBalanceAsync(Address address, Currency currency) =>
            Task.Run(() => blocks.GetBalance(address, currency));

        #region Mono

        public void SendException(Exception exc)
        {
            //FIXME: Make more meaningful method
            return;
        }

        private void Awake()
        {
            ForceDotNet.Force();
            string parentDir = Path.GetDirectoryName(DefaultStoragePath);
            if (!Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }
            DeletePreviousStore();
        }

        protected void OnDestroy()
        {
            _onMakeTransactionSubject.Dispose();

            BlockRenderHandler.Instance.Stop();
            ActionRenderHandler.Instance.Stop();
            ActionUnrenderHandler.Instance.Stop();
            Dispose();
        }

        #endregion

        private void InitAgent(Action<bool> callback, PrivateKey privateKey, CommandLineOptions options)
        {
            var peers = options.Peers.Select(LoadPeer);
            var iceServerList = options.IceServers.Select(LoadIceServer).ToImmutableList();

            if (!iceServerList.Any())
            {
                iceServerList = new[] { LoadIceServer(DefaultIceServer) }.ToImmutableList();
            }

            var iceServers = iceServerList.Sample(1).ToImmutableList();

            var host = GetHost(options);
            var port = options.Port;
            var consoleSink = options.ConsoleSink;
            var storagePath = options.StoragePath ?? DefaultStoragePath;
            var storageType = options.StorageType;
            var development = options.Development;
            var genesisBlockPath = options.GenesisBlockPath;
            var appProtocolVersion = options.AppProtocolVersion is null
                ? default
                : Libplanet.Net.AppProtocolVersion.FromToken(options.AppProtocolVersion);
            AppProtocolVersion = appProtocolVersion.Version;
            var trustedAppProtocolVersionSigners = options.TrustedAppProtocolVersionSigners
                .Select(s => new PublicKey(ByteUtil.ParseHex(s)));
            Init(
                privateKey,
                storagePath,
                peers,
                iceServers,
                host,
                port,
                consoleSink,
                development,
                appProtocolVersion,
                trustedAppProtocolVersionSigners,
                storageType,
                genesisBlockPath
            );

            // 별도 쓰레드에서는 GameObject.GetComponent<T> 를 사용할 수 없기때문에 미리 선언.
            var loadingScreen = Widget.Find<PreloadingScreen>();
            BootstrapStarted += (_, state) =>
                loadingScreen.Message = L10nManager.Localize("UI_LOADING_BOOTSTRAP_START");
            PreloadProcessed += (_, state) =>
            {
                if (loadingScreen)
                {
                    loadingScreen.Message = GetLoadingScreenMessage(state);
                }
            };

            PreloadEndedAsync += async () =>
            {
                // 에이전트의 상태를 한 번 동기화 한다.
                Currency goldCurrency =
                    new GoldCurrencyState((Dictionary)await GetStateAsync(GoldCurrencyState.Address)).Currency;
                await States.Instance.SetAgentStateAsync(
                    await GetStateAsync(Address) is Bencodex.Types.Dictionary agentDict
                        ? new AgentState(agentDict)
                        : new AgentState(Address));
                States.Instance.SetGoldBalanceState(new GoldBalanceState(Address,
                    await GetBalanceAsync(Address, goldCurrency)));
                States.Instance.SetCrystalBalance(
                    await GetBalanceAsync(Address, CrystalCalculator.CRYSTAL));
                if (await GetStateAsync(StakeState.DeriveAddress(States.Instance.AgentState.address)) is Dictionary stakeDict)
                {
                    var stakingState = new StakeState(stakeDict);
                    var level =
                        Game.TableSheets.Instance.StakeRegularRewardSheet
                            .FindLevelByStakedAmount(
                                Address,
                                await GetBalanceAsync(stakingState.address, CrystalCalculator.CRYSTAL));
                    States.Instance.SetStakeState(stakingState, level);
                }
                else
                {
                    var monsterCollectionAddress = MonsterCollectionState.DeriveAddress(
                        Address,
                        States.Instance.AgentState.MonsterCollectionRound
                    );
                    if (await GetStateAsync(monsterCollectionAddress) is Dictionary mcDict)
                    {
                        var monsterCollectionState = new MonsterCollectionState(mcDict);
                        States.Instance.SetMonsterCollectionState(monsterCollectionState);
                    }
                }

                ActionRenderHandler.Instance.GoldCurrency = goldCurrency;
                if (await GetStateAsync(GameConfigState.Address) is Dictionary configDict)
                {
                    States.Instance.SetGameConfigState(new GameConfigState(configDict));
                }
                else
                {
                    throw new FailedToInstantiateStateException<GameConfigState>();
                }

                var weeklyArenaState = await ArenaHelperOld.GetThisWeekStateAsync(BlockIndex);
                if (weeklyArenaState is null)
                {
                    throw new FailedToInstantiateStateException<WeeklyArenaState>();
                }

                States.Instance.SetWeeklyArenaState(weeklyArenaState);

                // 그리고 모든 액션에 대한 랜더와 언랜더를 핸들링하기 시작한다.
                BlockRenderHandler.Instance.Start(BlockRenderer);
                ActionRenderHandler.Instance.Start(ActionRenderer);
                ActionUnrenderHandler.Instance.Start(ActionRenderer);

                // 그리고 마이닝을 시작한다.
                StartNullableCoroutine(_miner);
                StartCoroutine(CoCheckBlockTip());

                StartNullableCoroutine(_autoPlayer);
                callback(SyncSucceed);
                LoadQueuedActions();
                TipChanged += (___, index) => { BlockIndexSubject.OnNext(index); };
            };

            _miner = options.NoMiner ? null : CoMiner();
            _autoPlayer = options.AutoPlay ? CoAutoPlayer() : null;

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
                    Widget.Find<IconAndButtonSystem>().ShowByBlockDownloadFail(current);
                    break;
                }
            }
        }

        private static string GetHost(CommandLineOptions options)
        {
            return string.IsNullOrEmpty(options.Host) ? null : options.Host;
        }

        private static BoundPeer LoadPeer(string peerInfo)
        {
            var tokens = peerInfo.Split(',');
            var pubKey = new PublicKey(ByteUtil.ParseHex(tokens[0]));
            var host = tokens[1];
            var port = int.Parse(tokens[2]);

            return new BoundPeer(pubKey, new DnsEndPoint(host, port));
        }

        private static IceServer LoadIceServer(string iceServerInfo)
        {
            var uri = new Uri(iceServerInfo);
            string[] userInfo = uri.UserInfo.Split(':');

            return new IceServer(new[] {uri}, userInfo[0], userInfo[1]);
        }

        private static BaseStore LoadStore(string path, string storageType)
        {
            BaseStore store = null;

            if (storageType is null)
            {
                Debug.Log("Storage Type is not specified. DefaultStore will be used.");
            }
            else if (storageType == "rocksdb")
            {
                try
                {
                    store = new RocksDBStore(path);
                    Debug.Log("RocksDB is initialized.");
                }
                catch (TypeInitializationException e)
                {
                    Debug.LogErrorFormat("RocksDB is not available. DefaultStore will be used. {0}", e);
                }
            }
            else
            {
                Debug.Log($"Storage Type {storageType} is not supported. DefaultStore will be used.");
            }

            return store ?? new DefaultStore(path, flush: false);
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

        private void DifferentAppProtocolVersionEncountered(
            Peer peer,
            AppProtocolVersion peerVersion,
            AppProtocolVersion localVersion
        )
        {
            // TODO: 론처 쪽 코드의 LibplanetController.NewAppProtocolVersionEncountered() 메서드와 기본적인
            // 로직은 같지만 구체적으로 취해야 할 액션이 크게 달라서 코드 공유를 하지 못하고 있음. 판단 로직과 판단에 따른
            // 행동 로직을 분리해서 판단 부분은 코드를 공유할 필요가 있음.
            Debug.LogWarningFormat(
                "Different Version Encountered; expected (local): {0}; actual ({1}): {2}",
                localVersion, peer, peerVersion
            );
            if (localVersion.Version < peerVersion.Version)
            {
                // 위 조건에 해당하지 않을 때는 true를 넣는 것이 아니라 no-op이어야 함.
                // (이 콜백 함수 자체가 여러 차례 호출될 수 있기 때문에 SyncSucceed가 false로 채워졌는데
                // 그 다음에 다시 true로 덮어씌어지거나 하면 안되기 때문.)
                SyncSucceed = false;
            }

            if (peerVersion.Version > EncounteredHighestVersion.Version)
            {
                EncounteredHighestVersion = peerVersion;
            }
        }

        private IEnumerator CoLogger()
        {
            Widget.Create<BattleSimulator>(true);
            Widget.Create<CombinationSimulator>(true);
            Widget.Create<Cheat>(true);
#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            Widget.Create<TestbedEditor>(true);
#endif
            while (true)
            {
                Cheat.Display("Logs", _tipInfo);

                StringBuilder log = new StringBuilder($"Last 10 tips :\n");
                foreach(var (block, appendedTime) in lastTenBlocks.ToArray().Reverse())
                {
                    log.Append($"[{block.Index}] {block.Hash}\n");
                    log.Append($" -Miner : {block.Miner.ToString()}\n");
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
                    var errorMsg = string.Format(L10nManager.Localize("UI_ERROR_FORMAT"),
                        L10nManager.Localize("BOOTSTRAP_FAIL"));

                    Widget.Find<TitleOneButtonSystem>().Show(
                        L10nManager.Localize("UI_ERROR"),
                        errorMsg,
                        L10nManager.Localize("UI_QUIT"),
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
            }

            yield return PreloadEndedAsync?.Invoke().ToCoroutine();

            var swarmStartTask = Task.Run(async () =>
            {
                try
                {
                    await _swarm.StartAsync();
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

                BlockRenderer.BlockSubject
                    .ObserveOnMainThread()
                    .Subscribe(TipChangedHandler);

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

        private void TipChangedHandler((
            Block<NCAction> OldTip,
            Block<NCAction> NewTip) tuple)
        {
            var (oldTip, newTip) = tuple;

            _tipInfo = "Tip Information\n";
            _tipInfo += $" -Miner           : {blocks.Tip.Miner.ToString()}\n";
            _tipInfo += $" -TimeStamp  : {DateTimeOffset.Now}\n";
            _tipInfo += $" -PrevBlock    : [{oldTip.Index}] {oldTip.Hash}\n";
            _tipInfo += $" -LatestBlock : [{newTip.Index}] {newTip.Hash}";
            while (lastTenBlocks.Count >= 10)
            {
                lastTenBlocks.TryDequeue(out _);
            }

            lastTenBlocks.Enqueue((blocks.Tip, DateTimeOffset.UtcNow));
            TipChanged?.Invoke(null, newTip.Index);
            BlockTipHashSubject.OnNext(newTip.Hash);
        }

        private IEnumerator CoTxProcessor()
        {
            while (true)
            {
                yield return new WaitForSeconds(TxProcessInterval);

                var actions = new List<NCAction>();

                Debug.LogFormat("Try Dequeue Actions. Total Count: {0}", _queuedActions.Count);
                while (_queuedActions.TryDequeue(out NCAction action))
                {
                    actions.Add(action);
                    Debug.LogFormat("Remain Queued Actions Count: {0}", _queuedActions.Count);
                }

                Debug.LogFormat("Finish Dequeue Actions.");

                if (actions.Any())
                {
                    var task = Task.Run(() => MakeTransaction(actions));
                    yield return new WaitUntil(() => task.IsCompleted);
                    foreach (var action in actions)
                    {
                        var ga = (GameAction) action.InnerAction;
                        _transactions.TryAdd(ga.Id, task.Result.Id);
                    }
                }
            }
        }

        private IEnumerator CoAutoPlayer()
        {
            var avatarIndex = 0;
            var dummyName = Address.ToHex().Substring(0, 8);

            yield return Game.Game.instance.ActionManager
                .CreateAvatar(avatarIndex, dummyName)
                .ToYieldInstruction();
            var avatarAddress = Address.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CreateAvatar2.DeriveFormat,
                    avatarIndex
                )
            );
            Debug.LogFormat("Autoplay[{0}, {1}]: CreateAvatar", avatarAddress.ToHex(), dummyName);

            yield return States.Instance.SelectAvatarAsync(avatarIndex).ToCoroutine();
            var waitForSeconds = new WaitForSeconds(TxProcessInterval);

            while (true)
            {
                yield return waitForSeconds;
                yield return Game.Game.instance.ActionManager.HackAndSlash(
                    new List<Costume>(),
                    new List<Equipment>(),
                    new List<Consumable>(),
                    1,
                    1).StartAsCoroutine();
                Debug.LogFormat("Autoplay[{0}, {1}]: HackAndSlash", avatarAddress.ToHex(), dummyName);
            }
        }

        private IEnumerator CoMiner()
        {
            var miner = new Miner(blocks, _swarm, PrivateKey);
            var sleepInterval = new WaitForSeconds(15);
            while (true)
            {
                var task = Task.Run(async() => await miner.MineBlockAsync(_cancellationTokenSource.Token));
                yield return new WaitUntil(() => task.IsCompleted);
#if UNITY_EDITOR
                yield return sleepInterval;
#endif
            }
        }

        private Transaction<NCAction> MakeTransaction(List<NCAction> actions)
        {
            var polymorphicActions = actions.ToArray();
            Debug.LogFormat("Make Transaction with Actions: `{0}`",
                string.Join(",", polymorphicActions.Select(i => i.InnerAction)));
            Transaction<NCAction> tx = blocks.MakeTransaction(PrivateKey, polymorphicActions);
            _onMakeTransactionSubject.OnNext((tx, actions));
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
            // 백업 저장소 지우는 데에 시간이 꽤 걸리기 때문에 백그라운드 잡으로 스폰
            Task.Run(() =>
            {
                StoreUtils.ClearBackupStores(DefaultStoragePath);
            });
        }

        private IEnumerator CoCheckStagedTxs()
        {
            var hasOwnTx = false;
            while (true)
            {
                // 프레임 저하를 막기 위해 별도 스레드로 처리합니다.
                Task<List<Transaction<NCAction>>> getOwnTxs =
                    Task.Run(
                        () => _stagePolicy.Iterate(blocks)
                            .Where(tx => tx.Signer.Equals(Address))
                            .ToList()
                    );

                yield return new WaitUntil(() => getOwnTxs.IsCompleted);

                if (!getOwnTxs.IsFaulted)
                {
                    List<Transaction<NCAction>> txs = getOwnTxs.Result;
                    var next = txs.Any();
                    if (next != hasOwnTx)
                    {
                        hasOwnTx = next;
                        OnHasOwnTx?.Invoke(hasOwnTx);
                    }
                }

                yield return new WaitForSeconds(.3f);
            }
        }

        private string GetLoadingScreenMessage(PreloadState state)
        {
            string localizationKey;
            long count;
            long totalCount;

            switch (state)
            {
                case BlockHashDownloadState blockHashDownloadState:
                    localizationKey = "UI_LOADING_BLOCK_HASH_DOWNLOAD";
                    count = blockHashDownloadState.ReceivedBlockHashCount;
                    totalCount = blockHashDownloadState.EstimatedTotalBlockHashCount;
                    break;

                case BlockDownloadState blockDownloadState:
                    localizationKey = "UI_LOADING_BLOCK_DOWNLOAD";
                    count = blockDownloadState.ReceivedBlockCount;
                    totalCount = blockDownloadState.TotalBlockCount;
                    break;

                case BlockVerificationState blockVerificationState:
                    localizationKey = "UI_LOADING_BLOCK_VERIFICATION";
                    count = blockVerificationState.VerifiedBlockCount;
                    totalCount = blockVerificationState.TotalBlockCount;
                    break;

                case StateDownloadState stateReferenceDownloadState:
                    localizationKey = "UI_LOADING_STATE_REFERENCE_DOWNLOAD";
                    count = stateReferenceDownloadState.ReceivedIterationCount;
                    totalCount = stateReferenceDownloadState.TotalIterationCount;
                    break;

                case ActionExecutionState actionExecutionState:
                    localizationKey = "UI_LOADING_ACTION_EXECUTE";
                    count = actionExecutionState.ExecutedBlockCount;
                    totalCount = actionExecutionState.TotalBlockCount;
                    break;

                default:
                    throw new Exception("Unknown state was reported during preload.");
            }

            string format = L10nManager.Localize(localizationKey);
            string text = string.Format(format, count, totalCount);
            return $"{text}  ({state.CurrentPhase} / {PreloadState.TotalPhase})";
        }
    }
}
