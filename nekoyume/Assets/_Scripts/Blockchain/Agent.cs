using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncIO;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c.Renderers;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blockchain.Renderers;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.RocksDBStore;
using Libplanet.Store;
using Libplanet.Store.Trie;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Nekoyume.Action;
using Nekoyume.Action.Loader;
using Nekoyume.Blockchain.Policy;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.Serilog;
using Nekoyume.State;
using Nekoyume.UI;
using NetMQ;
using Serilog;
using UnityEngine;
using NCTx = Libplanet.Types.Tx.Transaction;

namespace Nekoyume.Blockchain
{
    using System.Runtime.InteropServices;
    using UniRx;

    /// <summary>
    /// 블록체인 노드 관련 로직을 처리
    /// </summary>
    public class Agent : MonoBehaviour, IDisposable, IAgent
    {
        public static string DefaultStoragePath;

        // This key is hard-coded key so should not be used personally.
        public static readonly PrivateKey ProposerKey =
            new ("9f38eca075b9e2611a7e7a27291c081ee3fd570e88edd396bd09f292876f21c0");

        public Subject<long> BlockIndexSubject { get; } = new Subject<long>();
        public Subject<BlockHash> BlockTipHashSubject { get; } = new Subject<BlockHash>();

        private static IEnumerator _miner;
        private static IEnumerator _txProcessor;

        private static IEnumerator _swarmRunner;
        private static IEnumerator _autoPlayer;
        private static IEnumerator _logger;
        private const float TxProcessInterval = 3.0f;
        private const string QueuedActionsFileName = "queued_actions.dat";

        private readonly ConcurrentQueue<ActionBase> _queuedActions =
            new ConcurrentQueue<ActionBase>();

        private readonly TransactionMap _transactions = new TransactionMap(20);

        protected BlockChain blocks;
        protected BaseStore store;
        private IStagePolicy _stagePolicy;
        private IStateStore _stateStore;

        private static CancellationTokenSource _cancellationTokenSource;

        private string _tipInfo = string.Empty;

        private ConcurrentQueue<(Block, DateTimeOffset)> lastTenBlocks;

        public long BlockIndex => blocks?.Tip?.Index ?? 0;
        public PrivateKey PrivateKey { get; private set; }
        public Address Address => PrivateKey.Address;

        public BlockRenderer BlockRenderer { get; } = new BlockRenderer();

        public ActionRenderer ActionRenderer { get; } = new ActionRenderer();
        public int AppProtocolVersion { get; private set; }
        public BlockHash BlockTipHash => blocks.Tip.Hash;

        public HashDigest<SHA256> BlockTipStateRootHash => blocks.Tip.StateRootHash;

        private readonly Subject<(NCTx tx, List<ActionBase> actions)> _onMakeTransactionSubject =
            new Subject<(NCTx tx, List<ActionBase> actions)>();

        public IObservable<(NCTx tx, List<ActionBase> actions)> OnMakeTransaction => _onMakeTransactionSubject;

        public event EventHandler BootstrapStarted;
        public event Func<UniTask> PreloadEndedAsync;
        public event EventHandler<long> TipChanged;
        public static event Action<Guid> OnEnqueueOwnGameAction;
        public static event Action<bool> OnHasOwnTx;

        private bool SyncSucceed { get; set; }

        private static TelemetryClient _telemetryClient;

        private const string InstrumentationKey = "953da29a-95f7-4f04-9efe-d48c42a1b53a";

        public bool disposed;

        public IEnumerator Initialize(
            CommandLineOptions options,
            PrivateKey privateKey,
            Action<bool> callback)
        {
            NcDebug.Log($"[Agent] Start initialization");
            if (disposed)
            {
                NcDebug.Log("Agent Exist");
                yield break;
            }

            InitAgentAsync(callback, privateKey, options);
            NcDebug.Log($"[Agent] Finish initialization");
        }

        private async Task InitAsync(
            PrivateKey privateKey,
            string path,
            bool consoleSink,
            bool development,
            string storageType = null,
            string genesisBlockPath = null)
        {
            InitializeLogger(consoleSink, development);
            var genesisBlock = BlockManager.ImportBlock(genesisBlockPath ?? BlockManager.GenesisBlockPath());
            if (genesisBlock is null)
            {
                NcDebug.LogError("There is no genesis block.");
            }

            NcDebug.Log($"Store Path: {path}");
            NcDebug.Log($"Genesis Block Hash: {genesisBlock.Hash}");

            _stagePolicy = new VolatileStagePolicy();
            PrivateKey = privateKey;

            store = LoadStore(path, storageType);

            try
            {
                string keyPath = path + "/states";
                IKeyValueStore stateKeyValueStore = new DefaultKeyValueStore(keyPath);
                _stateStore = new TrieStateStore(stateKeyValueStore);
                var actionLoader = new NCActionLoader();
                var blockChainStates = new BlockChainStates(store, _stateStore);
                var policy = new BlockPolicySource().GetPolicy();
                var actionEvaluator = new ActionEvaluator(
                    _ => policy.BlockAction,
                    _stateStore,
                    actionLoader);

                if (store.GetCanonicalChainId() is null)
                {
                    blocks = BlockChain.Create(
                        policy,
                        _stagePolicy,
                        store,
                        _stateStore,
                        genesisBlock,
                        actionEvaluator,
                        renderers: new IRenderer[]{ BlockRenderer, ActionRenderer });
                }
                else
                {
                    blocks = new BlockChain(
                        policy,
                        _stagePolicy,
                        store,
                        _stateStore,
                        genesisBlock,
                        blockChainStates,
                        actionEvaluator,
                        renderers: new IRenderer[]{ BlockRenderer, ActionRenderer });
                }
            }
            catch (InvalidGenesisBlockException)
            {
                var popup = Widget.Find<IconAndButtonSystem>();
                if (Util.GetKeystoreJson() != string.Empty)
                {
                    popup.ShowWithTwoButton(
                        "UI_RESET_STORE",
                        "UI_RESET_STORE_CONTENT",
                        "UI_OK",
                        "UI_KEY_BACKUP",
                        true,
                        IconAndButtonSystem.SystemType.Information);
                    popup.SetCancelCallbackToBackup();
                }
                else
                {
                    popup.Show("UI_RESET_STORE", "UI_RESET_STORE_CONTENT");
                    popup.SetConfirmCallbackToExit();
                }
            }

#if BLOCK_LOG_USE
            FileHelper.WriteAllText("Block.log", "");
#endif
            lastTenBlocks = new ConcurrentQueue<(Block, DateTimeOffset)>();

            if (!consoleSink) InitializeTelemetryClient(privateKey.Address);

            // Init SyncSucceed
            SyncSucceed = true;

            _cancellationTokenSource = new CancellationTokenSource();
        }


        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            // `_swarm`의 내부 큐가 비워진 다음 완전히 종료할 때까지 더 기다립니다.
            Task.Run(() => {
                try
                {
                    store?.Dispose();
                    if (_stateStore is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch (ObjectDisposedException)
                {
                }
            });

            SaveQueuedActions();
            disposed = true;
        }

        public void EnqueueAction(ActionBase actionBase)
        {
            NcDebug.LogFormat("Enqueue GameAction: {0}", actionBase);
            _queuedActions.Enqueue(actionBase);

            if (actionBase is GameAction gameAction)
            {
                OnEnqueueOwnGameAction?.Invoke(gameAction.Id);
            }
        }

        public IValue GetState(Address accountAddress, Address address) =>
            blocks.GetWorldState().GetAccountState(accountAddress).GetState(accountAddress);

        public IValue GetState(HashDigest<SHA256> stateRootHash, Address accountAddress, Address address) =>
            blocks.GetWorldState(stateRootHash).GetAccountState(accountAddress).GetState(address);

        public async Task<IValue> GetStateAsync(Address accountAddress, Address address) =>
            await Task.FromResult(blocks.GetWorldState().GetAccountState(accountAddress).GetState(address));

        public Task<IValue> GetStateAsync(long blockIndex, Address accountAddress, Address address) =>
            throw new NotImplementedException($"{nameof(blockIndex)} is not supported yet.");

        public async Task<IValue> GetStateAsync(BlockHash blockHash, Address accountAddress, Address address) =>
            await Task.FromResult(blocks.GetWorldState(blockHash).GetAccountState(accountAddress).GetState(address));

        public async Task<IValue> GetStateAsync(HashDigest<SHA256> stateRootHash, Address accountAddress, Address address) =>
            await Task.FromResult(blocks.GetWorldState(stateRootHash).GetAccountState(accountAddress).GetState(address));

        public async Task<AgentState> GetAgentStateAsync(Address address) =>
            await Task.FromResult(blocks.GetWorldState().GetAgentState(address));

        public Task<AgentState> GetAgentStateAsync(long blockIndex, Address address) =>
            throw new NotImplementedException($"{nameof(blockIndex)} is not supported yet.");

        public async Task<AgentState> GetAgentStateAsync(HashDigest<SHA256> stateRootHash, Address address) =>
            await Task.FromResult(blocks.GetWorldState(stateRootHash).GetAgentState(address));

        public async Task<Dictionary<Address, AvatarState>> GetAvatarStatesAsync(
            IEnumerable<Address> addressList)
        {
            var dict = new Dictionary<Address, AvatarState>();
            foreach (var address in addressList)
            {
                try
                {
                    dict[address] = await Task.FromResult(blocks.GetWorldState().GetAvatarState(address));
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            return dict;
        }

        public Task<Dictionary<Address, AvatarState>> GetAvatarStatesAsync(
            long blockIndex,
            IEnumerable<Address> addressList) =>
            throw new NotImplementedException($"{nameof(blockIndex)} is not supported yet.");

        public async Task<Dictionary<Address, AvatarState>> GetAvatarStatesAsync(
            HashDigest<SHA256> stateRootHash,
            IEnumerable<Address> addressList)
        {
            var dict = new Dictionary<Address, AvatarState>();
            foreach (var address in addressList)
            {
                try
                {
                    dict[address] = await Task.FromResult(blocks.GetWorldState(stateRootHash).GetAvatarState(address));
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            return dict;
        }

        public async Task<Dictionary<Address, IValue>> GetStateBulkAsync(Address accountAddress, IEnumerable<Address> addressList)
        {
            var dict = new Dictionary<Address, IValue>();
            foreach (var address in addressList)
            {
                var result = await await Task.FromResult(GetStateAsync(accountAddress, address));
                dict[address] = result;
            }

            return dict;
        }

        public async Task<Dictionary<Address, IValue>> GetStateBulkAsync(
            HashDigest<SHA256> stateRootHash,
            Address accountAddress,
            IEnumerable<Address> addressList)
        {
            var dict = new Dictionary<Address, IValue>();
            foreach (var address in addressList)
            {
                var result = await await Task.FromResult(GetStateAsync(stateRootHash, accountAddress, address));
                dict[address] = result;
            }

            return dict;
        }

        public async Task<Dictionary<Address, IValue>> GetSheetsAsync(IEnumerable<Address> addressList)
        {
            var dict = new Dictionary<Address, IValue>();
            foreach (var address in addressList)
            {
                var result = await await Task.FromResult(GetStateAsync(
                    ReservedAddresses.LegacyAccount,
                    address));
                dict[address] = result;
            }

            return dict;
        }

        public bool TryGetTxId(Guid actionId, out TxId txId) =>
            _transactions.TryGetValue(actionId, out txId);

        public UniTask<bool> IsTxStagedAsync(TxId txId) =>
            UniTask.FromResult(blocks.GetStagedTransactionIds().Contains(txId));

        public FungibleAssetValue GetBalance(Address address, Currency currency) =>
            blocks.GetWorldState().GetBalance(address, currency);

        public async Task<FungibleAssetValue> GetBalanceAsync(
            Address address,
            Currency currency) =>
            await Task.FromResult(blocks.GetWorldState().GetBalance(address, currency));

        public Task<FungibleAssetValue> GetBalanceAsync(
            long blockIndex,
            Address address,
            Currency currency) =>
            throw new NotImplementedException($"{nameof(blockIndex)} is not supported yet.");

        public async Task<FungibleAssetValue> GetBalanceAsync(
            BlockHash blockHash,
            Address address,
            Currency currency) =>
            await Task.FromResult(blocks.GetWorldState(blockHash).GetBalance(address, currency));

        public async Task<FungibleAssetValue> GetBalanceAsync(
            HashDigest<SHA256> stateRootHash,
            Address address,
            Currency currency) =>
            await Task.FromResult(blocks.GetWorldState(stateRootHash).GetBalance(address, currency));

        // TODO: Below `GetInitState` codes have to be removed with Libplanet changes,
        // since using BlockChain.GetNextWorldState() is not recommended.
        // These have to be done by render from constructor of BlockChain in the future.
        public async Task<IValue> GetInitStateAsync(Address accountAddress, Address address) =>
            await Task.FromResult(blocks.GetNextWorldState().GetAccountState(accountAddress).GetState(address));

        public async Task<AgentState> GetInitAgentStateAsync(Address address) =>
            await Task.FromResult(blocks.GetNextWorldState().GetAgentState(address));

        public async Task<FungibleAssetValue> GetInitBalanceAsync(
            Address address,
            Currency currency) =>
            await Task.FromResult(blocks.GetNextWorldState().GetBalance(address, currency));

        #region Mono

        public void SendException(Exception exc)
        {
            //FIXME: Make more meaningful method
            return;
        }

        private void Awake()
        {
            PrepareForNativeLib_256K1();
            // Move static constructor to there to avoid conflict of native library loading
            try
            {
                // Avoid to construct repeatly
                if(Libplanet.Crypto.CryptoConfig.CryptoBackend is not Secp256K1CryptoBackend<SHA256>)
                {
                    Libplanet.Crypto.CryptoConfig.CryptoBackend = new Secp256K1CryptoBackend<SHA256>();
                }
            }
            catch (Exception e)
            {
                NcDebug.Log("Secp256K1CryptoBackend initialize failed. Use default backend.");
                NcDebug.LogException(e);
            }

            DefaultStoragePath = StorePath.GetDefaultStoragePath();

            ForceDotNet.Force();

            string parentDir = Path.GetDirectoryName(DefaultStoragePath);
            if (!Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            DeletePreviousStore();
        }

        private void PrepareForNativeLib_256K1()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                string Path_256K1 = default;
                OSPlatform os = default;
                Architecture arc = default;
                Path_256K1 = Application.dataPath.Split("/base.apk")[0];
                Path_256K1 = Path.Combine(Path_256K1, "lib");
                Path_256K1 = Path.Combine(Path_256K1, Environment.Is64BitProcess ? "arm64" : "arm");
                Path_256K1 = Path.Combine(Path_256K1, "libsecp256k1.so");
                os = OSPlatform.Linux;
                arc = Environment.Is64BitProcess ? Architecture.Arm64 : Architecture.Arm;
                // Load native library for secp256k1
                Secp256k1Net.UnityPathHelper.SetSpecificPath(Path_256K1, os, arc);
            }
        }

        protected void OnDestroy()
        {
            _onMakeTransactionSubject.Dispose();

            BlockRenderHandler.Instance.Stop();
            ActionRenderHandler.Instance.Stop();
            Dispose();
        }

        #endregion

        // TODO: Below initialization would be better to done by render from constructor of BlockChain,
        // after Libplanet support.
        private async void InitAgentAsync(Action<bool> callback, PrivateKey privateKey, CommandLineOptions options)
        {
            var consoleSink = options.ConsoleSink;
            var storagePath = options.StoragePath ?? DefaultStoragePath;
            var storageType = options.StorageType;
            var development = options.Development;
            var genesisBlockPath = options.GenesisBlockPath;
            await InitAsync(
                privateKey,
                storagePath,
                consoleSink,
                development,
                storageType,
                genesisBlockPath
            );

            // 별도 쓰레드에서는 GameObject.GetComponent<T> 를 사용할 수 없기때문에 미리 선언.
            // var loadingScreen = Widget.Find<PreloadingScreen>();

            PreloadEndedAsync += async () =>
            {
                // 에이전트의 상태를 한 번 동기화 한다.
                var goldCurrency = new GoldCurrencyState(
                    (Dictionary)await GetInitStateAsync(ReservedAddresses.LegacyAccount, GoldCurrencyState.Address)
                ).Currency;
                ActionRenderHandler.Instance.GoldCurrency = goldCurrency;

                await States.Instance.SetAgentStateAsync(
                    await GetInitAgentStateAsync(Address) ?? new AgentState(Address));
                States.Instance.SetGoldBalanceState(
                    new GoldBalanceState(Address,
                    await GetInitBalanceAsync(Address, goldCurrency)));
                States.Instance.SetCrystalBalance(
                    await GetInitBalanceAsync(Address, CrystalCalculator.CRYSTAL));

                if (await GetInitStateAsync(ReservedAddresses.LegacyAccount, GameConfigState.Address) is Dictionary configDict)
                {
                    States.Instance.SetGameConfigState(new GameConfigState(configDict));
                }
                else
                {
                    throw new FailedToInstantiateStateException<GameConfigState>();
                }

                var agentAddress = States.Instance.AgentState.address;
                var pledgeAddress = agentAddress.GetPledgeAddress();
                Address? patronAddress = null;
                var approved = false;
                if (await GetInitStateAsync(ReservedAddresses.LegacyAccount, pledgeAddress) is List list)
                {
                    patronAddress = list[0].ToAddress();
                    approved = list[1].ToBoolean();
                }
                States.Instance.SetPledgeStates(patronAddress, approved);

                // 그리고 모든 액션에 대한 랜더와 언랜더를 핸들링하기 시작한다.
                BlockRenderHandler.Instance.Start(BlockRenderer);
                ActionRenderHandler.Instance.Start(ActionRenderer);

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

        private static BaseStore LoadStore(string path, string storageType)
        {
            BaseStore store = null;

            if (storageType is null)
            {
                NcDebug.Log("Storage Type is not specified. DefaultStore will be used.");
            }
            else if (storageType == "rocksdb")
            {
                try
                {
                    store = new RocksDBStore(path);
                    NcDebug.Log("RocksDB is initialized.");
                }
                catch (TypeInitializationException e)
                {
                    NcDebug.LogErrorFormat("RocksDB is not available. DefaultStore will be used. {0}", e);
                }
            }
            else
            {
                NcDebug.Log($"Storage Type {storageType} is not supported. DefaultStore will be used.");
            }

            return store ?? new DefaultStore(path);
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
                foreach (var (block, appendedTime) in lastTenBlocks.ToArray().Reverse())
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
            NcDebug.Log("PreloadEndedAsync=" + (PreloadEndedAsync == null ? "null" : "ok"));

            yield return PreloadEndedAsync?.Invoke().ToCoroutine();

            Task.Run(() =>
            {
                BlockRenderer.BlockSubject
                    .ObserveOnMainThread()
                    .Subscribe(TipChangedHandler);
            });
        }

        private void TipChangedHandler((
            Block OldTip,
            Block NewTip) tuple)
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

                var actions = new List<ActionBase>();

                NcDebug.LogFormat("Try Dequeue Actions. Total Count: {0}", _queuedActions.Count);
                while (_queuedActions.TryDequeue(out ActionBase action))
                {
                    actions.Add(action);
                    NcDebug.LogFormat("Remain Queued Actions Count: {0}", _queuedActions.Count);
                }

                NcDebug.LogFormat("Finish Dequeue Actions.");

                if (actions.Any())
                {
                    var task = Task.FromResult(MakeTransaction(actions));
                    yield return new WaitUntil(() => task.IsCompleted);
                    foreach (var action in actions)
                    {
                        if (action is GameAction gameAction)
                        {
                            _transactions.TryAdd(gameAction.Id, task.Result.Id);
                        }
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
                    CreateAvatar.DeriveFormat,
                    avatarIndex
                )
            );
            NcDebug.LogFormat("Autoplay[{0}, {1}]: CreateAvatar", avatarAddress.ToHex(), dummyName);

            yield return States.Instance.SelectAvatarAsync(avatarIndex).ToCoroutine();
            var waitForSeconds = new WaitForSeconds(TxProcessInterval);

            while (true)
            {
                yield return waitForSeconds;
                yield return Game.Game.instance.ActionManager.HackAndSlash(
                    new(),
                    new(),
                    new(),
                    new(),
                    1,
                    1).StartAsCoroutine();
                NcDebug.LogFormat("Autoplay[{0}, {1}]: HackAndSlash", avatarAddress.ToHex(), dummyName);
            }
        }

        private IEnumerator CoMiner()
        {
            var miner = new Proposer(blocks, ProposerKey);
            var sleepInterval = new WaitForSeconds(1);
            while (true)
            {
                var task = Task.FromResult(miner.ProposeBlockAsync(_cancellationTokenSource.Token));
                yield return new WaitUntil(() => task.IsCompleted);
#if UNITY_EDITOR
                yield return sleepInterval;
#endif
            }
        }

        private Transaction MakeTransaction(List<ActionBase> actions)
        {
            NcDebug.LogFormat("Make Transaction with Actions: `{0}`",
                string.Join(",", actions));
            Transaction tx = blocks.MakeTransaction(
                privateKey: PrivateKey,
                actions: actions
            );
            _onMakeTransactionSubject.OnNext((tx, actions));

            return tx;
        }

        private void LoadQueuedActions()
        {
            var path = Platform.GetPersistentDataPath(QueuedActionsFileName);
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

                    NcDebug.Log($"Load queued actions: {_queuedActions.Count}");
                    File.Delete(path);
                }
            }
        }

        private void SaveQueuedActions()
        {
            if (_queuedActions.Any())
            {
                List<GameAction> actionsList;
                var path = Platform.GetPersistentDataPath(QueuedActionsFileName);
                if (!File.Exists(path))
                {
                    NcDebug.Log("Create new queuedActions list.");
                    actionsList = new List<GameAction>();
                }
                else
                {
                    actionsList =
                        ByteSerializer.Deserialize<List<GameAction>>(File.ReadAllBytes(path));
                    NcDebug.Log($"Load queuedActions list. : {actionsList.Count}");
                }

                NcDebug.LogWarning($"Save QueuedActions : {_queuedActions.Count}");
                while (_queuedActions.TryDequeue(out var action))
                    actionsList.Add((GameAction)action);

                File.WriteAllBytes(path, ByteSerializer.Serialize(actionsList));
            }
        }

        private static void DeletePreviousStore()
        {
            // 백업 저장소 지우는 데에 시간이 꽤 걸리기 때문에 백그라운드 잡으로 스폰
            Task.Run(() => StoreUtils.ClearBackupStores(DefaultStoragePath));
        }

        private IEnumerator CoCheckStagedTxs()
        {
            var hasOwnTx = false;
            while (true)
            {
                // 프레임 저하를 막기 위해 별도 스레드로 처리합니다.
                Task<List<Transaction>> getOwnTxs =
                    Task.FromResult(_stagePolicy.Iterate(blocks)
                        .Where(tx => tx.Signer.Equals(Address))
                        .ToList());
                yield return new WaitUntil(() => getOwnTxs.IsCompleted);

                if (!getOwnTxs.IsFaulted)
                {
                    List<Transaction> txs = getOwnTxs.Result;
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
    }
}
