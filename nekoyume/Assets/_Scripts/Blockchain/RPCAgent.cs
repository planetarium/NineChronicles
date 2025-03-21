using System;
using System.Collections;
using System.Collections.Async;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Bencodex;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Grpc.Core;
using Ionic.Zlib;
using Lib9c;
using Lib9c.Renderers;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using LruCacheNet;
using MagicOnion;
using MagicOnion.Client;
using MagicOnion.Unity;
using MessagePack;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using Nekoyume.Shared.Hubs;
using Nekoyume.Shared.Services;
using Nekoyume.State;
using Nekoyume.UI;
using NineChronicles.RPC.Shared.Exceptions;
using UnityEngine;
using Debug = UnityEngine.Debug;
using NCTx = Libplanet.Types.Tx.Transaction;
using Random = System.Random;

namespace Nekoyume.Blockchain
{
    using System.Text;
    using UniRx;

    public class RPCAgent : MonoBehaviour, IAgent, IActionEvaluationHubReceiver
    {
        private const int RpcConnectionRetryCount = 6;
        private const float TxProcessInterval = 1.0f;
        private readonly ConcurrentQueue<(ActionBase, Func<TxId, Task<bool>>)> _queuedActions = new();

        private readonly TransactionMap _transactions = new(20);

        private GrpcChannelx _channel;

        private IActionEvaluationHub _hub;

        private IBlockChainService _service;

        private Codec _codec = new();

        private Block _genesis;

        private DateTimeOffset _lastTipChangedAt;

        public BlockRenderer BlockRenderer { get; } = new();

        public ActionRenderer ActionRenderer { get; } = new();

        public Subject<long> BlockIndexSubject { get; } = new();

        public Subject<BlockHash> BlockTipHashSubject { get; } = new();

        public long BlockIndex { get; private set; }

        public PrivateKey PrivateKey { get; private set; }

        public Address Address => PrivateKey.PublicKey.Address;

        public bool Connected { get; private set; }

        public readonly Subject<RPCAgent> OnDisconnected = new();

        public readonly Subject<RPCAgent> OnRetryStarted = new();

        public readonly Subject<RPCAgent> OnRetryEnded = new();

        public readonly Subject<RPCAgent> OnPreloadStarted = new();

        public readonly Subject<RPCAgent> OnPreloadEnded = new();

        public readonly Subject<(RPCAgent, int retryCount)> OnRetryAttempt = new();

        public readonly Subject<bool> OnTxStageEnded = new();

        public BlockHash BlockTipHash { get; private set; }

        public HashDigest<SHA256> BlockTipStateRootHash { get; private set; }

        private readonly Subject<(NCTx tx, List<ActionBase> actions)> _onMakeTransactionSubject = new();

        public IObservable<(NCTx tx, List<ActionBase> actions)> OnMakeTransaction => _onMakeTransactionSubject;

        private readonly List<IDisposable> _disposables = new();

        private readonly BlockHashCache _blockHashCache = new(100);
        private MessagePackSerializerOptions _lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        private readonly List<string> cachedRpcServerHosts = new();
        private int cachedRpcServerPort;
        private CancellationTokenSource cancellationTokenSource;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void OnRuntimeInitialize()
        {
            // Initialize gRPC channel provider when the application is loaded.
            GrpcChannelProviderHost.Initialize(new LoggingGrpcChannelProvider(
                new DefaultGrpcChannelProvider(new[]
                {
                    new ChannelOption("grpc.max_receive_message_length", -1),
                })
            ));
        }
        //
        // /// <summary>
        // /// Initialize without private key.
        // /// </summary>
        // /// <param name="options"></param>
        // /// <returns></returns>
        // public IEnumerator InitializeWithoutPrivateKey(
        //     CommandLineOptions options)
        // {
        //     _channel = GrpcChannelx.ForTarget(new GrpcChannelTarget(options.RpcServerHost, options.RpcServerPort, true));
        //     _lastTipChangedAt = DateTimeOffset.UtcNow;
        //     var connect = StreamingHubClient
        //         .ConnectAsync<IActionEvaluationHub, IActionEvaluationHubReceiver>(
        //             _channel,
        //             this)
        //         .AsCoroutine();
        //     yield return connect;
        //     _hub = connect.Result;
        //     _service = MagicOnionClient.Create<IBlockChainService>(_channel, new IClientFilter[]
        //     {
        //         new ClientFilter()
        //     });
        //
        //     // Android Mono only support arm7(32bit) backend in unity engine.
        //     // 1. System.Net.WebClient is invaild when use Android Mono in currnet unity version.
        //     // See this: https://issuetracker.unity3d.com/issues/system-dot-net-dot-webclient-not-working-when-building-on-android
        //     // 2. If we use WWW class as a workaround, unfortunately, this class can't be used in aysnc function.
        //     // So I can only use normal ImportBlock() function when build in Android Mono backend :(
        //     var task = Task.Run(async () =>
        //     {
        //         _genesis = await BlockManager.ImportBlockAsync(options.GenesisBlockPath ?? BlockManager.GenesisBlockPath());
        //     });
        //     yield return new WaitUntil(() => task.IsCompleted);
        // }

        public IEnumerator Initialize(
            CommandLineOptions options,
            PrivateKey privateKey,
            Action<bool> callback)
        {
            NcDebug.Log($"[RPCAgent] Start initialization: {options.RpcServerHost}:{options.RpcServerPort}");

            cachedRpcServerHosts.Clear();
            foreach (var rpcServerHost in options.RpcServerHosts)
            {
                cachedRpcServerHosts.Add(rpcServerHost);
            }

            cachedRpcServerPort = options.RpcServerPort;
            NcDebug.Log($"[RPCAgent] Cached RPC server hosts: \n{string.Join(",\n", cachedRpcServerHosts)}");

            PrivateKey = privateKey;
            _channel ??= GrpcChannelx.ForTarget(
                new GrpcChannelTarget(options.RpcServerHost, options.RpcServerPort, true));

            _lastTipChangedAt = DateTimeOffset.UtcNow;
            if (_hub == null)
            {
                var connect = StreamingHubClient
                    .ConnectAsync<IActionEvaluationHub, IActionEvaluationHubReceiver>(
                        _channel,
                        this)
                    .AsCoroutine();
                yield return connect;
                _hub = connect.Result;
            }

            _service ??= MagicOnionClient.Create<IBlockChainService>(_channel, new IClientFilter[]
            {
                new ClientFilter()
            });

            var getTipTask = UniTask.RunOnThreadPool(async () =>
            {
                var getTipTaskResult = await _service.GetTip();
                OnRenderBlock(null, getTipTaskResult);
            });

            if (_genesis == null)
            {
                var sw = new Stopwatch();
                sw.Reset();
                sw.Start();
                // Android Mono only support arm7(32bit) backend in unity engine.
                var architecture_is_32bit = !Environment.Is64BitProcess;
                var is_Android = Application.platform == RuntimePlatform.Android;
                if (is_Android && architecture_is_32bit)
                {
                    // 1. System.Net.WebClient is invaild when use Android Mono in currnet unity version.
                    // See this: https://issuetracker.unity3d.com/issues/system-dot-net-dot-webclient-not-working-when-building-on-android
                    // 2. If we use WWW class as a workaround, unfortunately, this class can't be used in aysnc function.
                    // So I can only use normal ImportBlock() function when build in Android Mono backend :(
                    _genesis = BlockManager.ImportBlock(null);
                }
                else
                {
                    yield return UniTask.Run(async () =>
                    {
                        var genesisBlockPath = options.GenesisBlockPath ?? BlockManager.GenesisBlockPath();
                        _genesis = await BlockManager.ImportBlockAsync(genesisBlockPath);
                    }).ToCoroutine();
                }

                sw.Stop();
                NcDebug.Log($"[RPCAgent] genesis block imported in {sw.ElapsedMilliseconds}ms.(elapsed)");
            }

            yield return getTipTask.ToCoroutine();
            RegisterDisconnectEvent(_hub);
            StartCoroutine(CoTxProcessor());
            StartCoroutine(CoJoin(callback));
            NcDebug.Log($"[RPCAgent] Finish initialization");
        }

        public IValue GetState(Address accountAddress, Address address)
        {
            var raw = _service.GetStateByBlockHash(
                BlockTipHash.ToByteArray(),
                accountAddress.ToByteArray(),
                address.ToByteArray()
            ).ResponseAsync.Result;
            return DeCompressState(raw);
        }

        public IValue GetState(HashDigest<SHA256> stateRootHash, Address accountAddress, Address address)
        {
            var raw = _service.GetStateByStateRootHash(
                stateRootHash.ToByteArray(),
                accountAddress.ToByteArray(),
                address.ToByteArray()
            ).ResponseAsync.Result;
            return DeCompressState(raw);
        }

        public async Task<IValue> GetStateAsync(Address accountAddress, Address address)
        {
            var game = Game.Game.instance;
            // Check state & cached because force update state after rpc disconnected.
            if (game.CachedStateAddresses.TryGetValue(accountAddress.Derive(address.ToByteArray()), out var cached) &&
                cached &&
                game.CachedStates.TryGetValue(accountAddress.Derive(address.ToByteArray()), out var value) &&
                value is not Null)
            {
                return value;
            }

            return await GetStateAsync(BlockTipStateRootHash, accountAddress, address);
        }

        public async Task<IValue> GetStateAsync(long blockIndex, Address accountAddress, Address address)
        {
            var blockHash = await GetBlockHashAsync(blockIndex);
            if (!blockHash.HasValue)
            {
                NcDebug.LogError($"Failed to get block hash from block index: {blockIndex}");
                return null;
            }

            return await GetStateAsync(blockHash.Value, accountAddress, address);
        }

        public async Task<IValue> GetStateAsync(BlockHash blockHash, Address accountAddress, Address address)
        {
            var bytes = await _service.GetStateByBlockHash(
                blockHash.ToByteArray(),
                accountAddress.ToByteArray(),
                address.ToByteArray());
            var decoded = DeCompressState(bytes);
            var game = Game.Game.instance;
            if (game.CachedStateAddresses.ContainsKey(accountAddress.Derive(address.ToByteArray())))
            {
                game.CachedStateAddresses[accountAddress.Derive(address.ToByteArray())] = true;
            }

            if (game.CachedStates.ContainsKey(accountAddress.Derive(address.ToByteArray())))
            {
                game.CachedStates.AddOrUpdate(accountAddress.Derive(address.ToByteArray()), decoded);
            }

            return decoded;
        }

        public async Task<IValue> GetStateAsync(HashDigest<SHA256> stateRootHash, Address accountAddress, Address address)
        {
            var bytes = await _service.GetStateByStateRootHash(
                stateRootHash.ToByteArray(),
                accountAddress.ToByteArray(),
                address.ToByteArray());
            var decoded = DeCompressState(bytes);
            var game = Game.Game.instance;
            if (game.CachedStateAddresses.ContainsKey(accountAddress.Derive(address.ToByteArray())))
            {
                game.CachedStateAddresses[accountAddress.Derive(address.ToByteArray())] = true;
            }

            if (game.CachedStates.ContainsKey(accountAddress.Derive(address.ToByteArray())))
            {
                game.CachedStates.AddOrUpdate(accountAddress.Derive(address.ToByteArray()), decoded);
            }

            return decoded;
        }

        public FungibleAssetValue GetBalance(Address addr, Currency currency)
        {
            return GetBalanceAsync(addr, currency).Result;
        }

        public async Task<FungibleAssetValue> GetBalanceAsync(
            Address addr,
            Currency currency)
        {
            var game = Game.Game.instance;
            if (game.CachedBalance.TryGetValue(currency, out var cache) &&
                cache.TryGetValue(addr, out var fav) &&
                !fav.Equals(default))
            {
                await Task.CompletedTask;
                return fav;
            }

            var balance = await GetBalanceAsync(BlockTipStateRootHash, addr, currency)
                .ConfigureAwait(false);
            if (addr.Equals(Address))
            {
                if (!game.CachedBalance.ContainsKey(currency))
                {
                    game.CachedBalance[currency] =
                        new LruCache<Address, FungibleAssetValue>(2);
                }

                game.CachedBalance[currency][addr] = balance;
            }

            return balance;
        }

        public async Task<FungibleAssetValue> GetBalanceAsync(
            long blockIndex,
            Address addr,
            Currency currency)
        {
            var game = Game.Game.instance;
            if (game.CachedBalance.TryGetValue(currency, out var cache) &&
                cache.TryGetValue(addr, out var fav) &&
                !fav.Equals(default))
            {
                await Task.CompletedTask;
                return fav;
            }

            var blockHash = await GetBlockHashAsync(blockIndex);
            if (blockHash is null)
            {
                NcDebug.LogError($"Failed to get block hash from block index: {blockIndex}");
                return 0 * currency;
            }

            var balance = await GetBalanceAsync(blockHash.Value, addr, currency)
                .ConfigureAwait(false);
            if (addr.Equals(Address))
            {
                if (!game.CachedBalance.ContainsKey(currency))
                {
                    game.CachedBalance[currency] =
                        new LruCache<Address, FungibleAssetValue>(2);
                }

                game.CachedBalance[currency][addr] = balance;
            }

            return balance;
        }

        public async Task<FungibleAssetValue> GetBalanceAsync(
            BlockHash blockHash,
            Address address,
            Currency currency)
        {
            var raw = await _service.GetBalanceByBlockHash(
                BlockTipHash.ToByteArray(),
                address.ToByteArray(),
                _codec.Encode(currency.Serialize()));
            var serialized = (List)DeCompressState(raw);
            return FungibleAssetValue.FromRawValue(
                new Currency(serialized.ElementAt(0)),
                serialized.ElementAt(1).ToBigInteger());
        }

        public async Task<FungibleAssetValue> GetBalanceAsync(
            HashDigest<SHA256> stateRootHash,
            Address address,
            Currency currency)
        {
            var raw = await _service.GetBalanceByStateRootHash(
                stateRootHash.ToByteArray(),
                address.ToByteArray(),
                _codec.Encode(currency.Serialize()));
            var serialized = (List)DeCompressState(raw);
            return FungibleAssetValue.FromRawValue(
                new Currency(serialized.ElementAt(0)),
                serialized.ElementAt(1).ToBigInteger());
        }

        public async Task<Integer> GetUnbondClaimableHeightByStateRootHashAsync(HashDigest<SHA256> stateRootHash, Address address)
        {
            var raw = await _service.GetUnbondClaimableHeightByStateRootHash(
                stateRootHash.ToByteArray(),
                address.ToByteArray());
            return (Integer)DeCompressState(raw);
        }

        /// <summary>
        /// Need Convert to FungibleAssetValue
        /// </summary>
        /// <param name="stateRootHash">stateRootHash</param>
        /// <param name="address">agentAddress</param>
        /// <returns>raw value list of fav</returns>
        public async Task<List> GetClaimableRewardsByStateRootHashAsync(HashDigest<SHA256> stateRootHash, Address address)
        {
            var raw = await _service.GetClaimableRewardsByStateRootHash(
                stateRootHash.ToByteArray(),
                address.ToByteArray());
            return (List)DeCompressState(raw);
        }

        /// <summary>
        /// List로 디코딩 후 3개의 정보로 분리됩니다:
        /// [0]: BigInteger - 유저의 지분값
        /// [1]: BigInteger - 총 지분값
        /// [2]: FungibleAssetValue - 총 위임값 (GuildGold로 표시)
        /// 총 위임값을 NCG로 환산하려면 Lib9c.GuildModule의 ConvertCurrency를 사용하세요.
        /// </summary>
        /// <param name="stateRootHash">stateRootHash</param>
        /// <param name="address">agentAddress</param>
        /// <returns>summary에 설명된 데이터가 담긴 List</returns>
        public async Task<List> GetDelegationInfoByStateRootHashAsync(HashDigest<SHA256> stateRootHash, Address address)
        {
            var raw = await _service.GetDelegationInfoByStateRootHash(
                stateRootHash.ToByteArray(),
                address.ToByteArray());
            return (List)DeCompressState(raw);
        }

        public async Task<FungibleAssetValue> GetStakedByStateRootHashAsync(HashDigest<SHA256> stateRootHash, Address address)
        {
            var raw = await _service.GetStakedByStateRootHash(
                stateRootHash.ToByteArray(),
                address.ToByteArray());
            var serialized = (List)DeCompressState(raw);
            return FungibleAssetValue.FromRawValue(
                new Currency(serialized.ElementAt(0)),
                serialized.ElementAt(1).ToBigInteger());
        }

        public async Task<AgentState> GetAgentStateAsync(Address address)
        {
            var raw = await _service.GetAgentStatesByStateRootHash(
                BlockTipStateRootHash.ToByteArray(),
                new[] { address.ToByteArray() });
            return ResolveAgentState(raw.Values.First());
        }

        public async Task<AgentState> GetAgentStateAsync(long blockIndex, Address address)
        {
            var blockHash = await GetBlockHashAsync(blockIndex);
            if (blockHash is not { } blockHashNotNull)
            {
                NcDebug.LogError($"Failed to get block hash from block index: {blockIndex}");
                return null;
            }

            var raw = await _service.GetAgentStatesByBlockHash(
                blockHashNotNull.ToByteArray(),
                new[] { address.ToByteArray() });
            return ResolveAgentState(raw.Values.First());
        }

        public async Task<AgentState> GetAgentStateAsync(HashDigest<SHA256> stateRootHash, Address address)
        {
            var raw = await _service.GetAgentStatesByStateRootHash(
                stateRootHash.ToByteArray(),
                new[] { address.ToByteArray() });
            return ResolveAgentState(raw.Values.First());
        }

        private AgentState ResolveAgentState(byte[] raw)
        {
            var value = DeCompressState(raw);
            if (value is Dictionary dict)
            {
                return new AgentState(dict);
            }
            else if (value is List list)
            {
                return new AgentState(list);
            }
            else
            {
                NcDebug.LogError("Given raw is not a format of the AgentState.");
                return null;
            }
        }

        public async Task<Dictionary<Address, AvatarState>> GetAvatarStatesAsync(
            IEnumerable<Address> addressList)
        {
            var raw = await _service.GetAvatarStatesByStateRootHash(
                BlockTipStateRootHash.ToByteArray(),
                addressList.Select(a => a.ToByteArray()));
            var result = new Dictionary<Address, AvatarState>();
            foreach (var kv in raw)
            {
                var size = Buffer.ByteLength(kv.Value);
                NcDebug.Log($"[GetAvatarState/{kv.Key}] buffer size: {size}");
                result[new Address(kv.Key)] = ResolveAvatarState(kv.Value);
            }

            return result;
        }

        public async Task<Dictionary<Address, AvatarState>> GetAvatarStatesAsync(
            long blockIndex,
            IEnumerable<Address> addressList)
        {
            var blockHash = await GetBlockHashAsync(blockIndex);
            if (!blockHash.HasValue)
            {
                NcDebug.LogError($"Failed to get block hash from block index: {blockIndex}");
                return null;
            }

            var raw = await _service.GetAvatarStatesByBlockHash(
                blockHash.Value.ToByteArray(),
                addressList.Select(a => a.ToByteArray()));
            var result = new Dictionary<Address, AvatarState>();
            foreach (var kv in raw)
            {
                result[new Address(kv.Key)] = ResolveAvatarState(kv.Value);
            }

            return result;
        }

        public async Task<Dictionary<Address, AvatarState>> GetAvatarStatesAsync(
            HashDigest<SHA256> stateRootHash,
            IEnumerable<Address> addressList)
        {
            var raw = await _service.GetAvatarStatesByStateRootHash(
                stateRootHash.ToByteArray(),
                addressList.Select(a => a.ToByteArray()));
            var result = new Dictionary<Address, AvatarState>();
            foreach (var kv in raw)
            {
                result[new Address(kv.Key)] = ResolveAvatarState(kv.Value);
            }

            return result;
        }

        private AvatarState ResolveAvatarState(byte[] raw)
        {
            AvatarState avatarState;
            if (!(DeCompressState(raw) is List full))
            {
                NcDebug.LogError("Given raw is not a format of the AvatarState.");
                return null;
            }

            if (full.Count == 1)
            {
                if (full[0] is Dictionary dict)
                {
                    avatarState = new AvatarState(dict);
                }
                else
                {
                    NcDebug.LogError("Given raw is not a format of the AvatarState.");
                    return null;
                }
            }
            else if (full.Count == 4)
            {
                if (full[0] is Dictionary dict)
                {
                    avatarState = new AvatarState(dict);
                }
                else if (full[0] is List list)
                {
                    avatarState = new AvatarState(list);
                }
                else
                {
                    NcDebug.LogError("Given raw is not a format of the AvatarState.");
                    return null;
                }

                if (full[1] is not List inventoryList)
                {
                    NcDebug.LogError("Given raw is not a format of the inventory.");
                    return null;
                }

                switch (full[2])
                {
                    case Dictionary questListDict:
                        avatarState.questList = new QuestList(questListDict);
                        break;
                    case List questList:
                        avatarState.questList = new QuestList(questList);
                        break;
                    default:
                        NcDebug.LogError("Given raw is not a format of the questList.");
                        return null;
                }

                if (full[3] is not Dictionary worldInformationDict)
                {
                    NcDebug.LogError("Given raw is not a format of the worldInformation.");
                    return null;
                }

                avatarState.inventory = new Inventory(inventoryList);
                avatarState.worldInformation = new WorldInformation(worldInformationDict);
            }
            else
            {
                NcDebug.LogError("Given raw is not a format of the AvatarState.");
                return null;
            }

            return avatarState;
        }

        public async Task<Dictionary<Address, IValue>> GetStateBulkAsync(
            HashDigest<SHA256> stateRootHash,
            Address accountAddress,
            IEnumerable<Address> addressList)
        {
            var raw =
                await _service.GetBulkStateByStateRootHash(
                    stateRootHash.ToByteArray(),
                    accountAddress.ToByteArray(),
                    addressList.Select(a => a.ToByteArray()));
            var result = new Dictionary<Address, IValue>();
            foreach (var kv in raw)
            {
                result[new Address(kv.Key)] = DeCompressState(kv.Value);
            }

            return result;
        }

        public async Task<Dictionary<Address, IValue>> GetSheetsAsync(IEnumerable<Address> addressList)
        {
            var sw = new Stopwatch();
            sw.Start();
            var cd = new ConcurrentDictionary<byte[], byte[]>();
            var chunks = addressList
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / 24)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
            await chunks
                .AsParallel()
                .ParallelForEachAsync(async list =>
                {
                    var raw =
                        await _service.GetSheets(
                            BlockTipStateRootHash.ToByteArray(),
                            list.Select(a => a.ToByteArray()));
                    await raw.ParallelForEachAsync(async pair =>
                    {
                        cd.TryAdd(pair.Key, pair.Value);
                        var size = Buffer.ByteLength(pair.Value);
                        NcDebug.Log($"[GetSheets/{new Address(pair.Key)}] buffer size: {size}");
                        await Task.CompletedTask;
                    });
                });
            sw.Stop();
            NcDebug.Log($"[SyncTableSheets/GetSheets] Get sheets. {sw.Elapsed}");
            sw.Restart();
            var result = new Dictionary<Address, IValue>();
            foreach (var kv in cd)
            {
                result[new Address(kv.Key)] = (Text)Encoding.UTF8.GetString(kv.Value);
            }

            sw.Stop();
            NcDebug.Log($"[SyncTableSheets/GetSheets] decode values. {sw.Elapsed}");
            return result;
        }

        public void SendException(Exception exc)
        {
        }

        public void EnqueueAction(ActionBase actionBase, Func<TxId, Task<bool>> onTxIdReceived = null)
        {
            _queuedActions.Enqueue((actionBase, onTxIdReceived));
        }

        #region Mono

        private void Awake()
        {
            Dictionary<string, Value> GetPlayerAddressForLogging()
            {
                var value = new Dictionary<string, Value>();
                if (States.Instance.AgentState is not null)
                {
                    value["AgentAddress"] = States.Instance.AgentState.address.ToString();
                }

                if (States.Instance.CurrentAvatarState is not null)
                {
                    value["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString();
                }

                return value;
            }

            OnDisconnected
                .ObserveOnMainThread()
                .Subscribe(_ => Analyzer.Instance?.Track("Unity/RPC Disconnected", GetPlayerAddressForLogging()))
                .AddTo(_disposables);
            OnRetryStarted
                .ObserveOnMainThread()
                .Subscribe(_ => Analyzer.Instance?.Track("Unity/RPC Retry Connect Started", GetPlayerAddressForLogging()))
                .AddTo(_disposables);
            OnRetryEnded
                .ObserveOnMainThread()
                .Subscribe(_ => Analyzer.Instance?.Track("Unity/RPC Retry Connect Ended", GetPlayerAddressForLogging()))
                .AddTo(_disposables);
            OnPreloadStarted
                .ObserveOnMainThread()
                .Subscribe(_ => Analyzer.Instance?.Track("Unity/RPC Preload Started", GetPlayerAddressForLogging()))
                .AddTo(_disposables);
            OnPreloadEnded
                .ObserveOnMainThread()
                .Subscribe(_ => Analyzer.Instance?.Track("Unity/RPC Preload Ended", GetPlayerAddressForLogging()))
                .AddTo(_disposables);
            OnRetryAttempt
                .ObserveOnMainThread()
                .Subscribe()
                .AddTo(_disposables);
            OnTxStageEnded
                .ObserveOnMainThread()
                .Subscribe(result =>
                {
                    if (!result)
                    {
                        var popup = Widget.Find<IconAndButtonSystem>();
                        popup.Show(L10nManager.Localize("UI_ERROR"),
                            L10nManager.Localize("UI_TX_STAGE_FAILED"), L10nManager.Localize("UI_OK"));
                        popup.SetConfirmCallbackToExit();
                    }
                })
                .AddTo(_disposables);
            Game.Event.OnUpdateAddresses.AddListener(UpdateSubscribeAddresses);

            cancellationTokenSource = new CancellationTokenSource();
        }

        private async void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
            _onMakeTransactionSubject.Dispose();

            BlockRenderHandler.Instance.Stop();
            ActionRenderHandler.Instance.Stop();

            StopAllCoroutines();
            if (_hub is not null)
            {
                await _hub.DisposeAsync();
            }

            if (_channel is not null)
            {
                await _channel?.ShutdownAsync();
            }

            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }

        #endregion

        private IEnumerator CoJoin(Action<bool> callback)
        {
            var t = Task.Run(async () => { await Join(); });

            yield return new WaitUntil(() => t.IsCompleted);

            if (t.IsFaulted)
            {
                callback?.Invoke(false);
                yield break;
            }

            Connected = true;

            // 에이전트의 상태를 한 번 동기화 한다.
            var currencyTask = Task.Run(async () =>
            {
                var goldCurrency = new GoldCurrencyState(
                    (Dictionary)await GetStateAsync(
                        ReservedAddresses.LegacyAccount,
                        GoldCurrencyState.Address)
                ).Currency;
                ActionRenderHandler.Instance.GoldCurrency = goldCurrency;

                await States.Instance.SetAgentStateAsync(
                    await GetAgentStateAsync(Address) ?? new AgentState(Address));
                States.Instance.SetGoldBalanceState(
                    new GoldBalanceState(
                        Address,
                        await GetBalanceAsync(Address, goldCurrency)));
                States.Instance.SetCrystalBalance(
                    await GetBalanceAsync(Address, Currencies.Crystal));

                if (await GetStateAsync(
                        ReservedAddresses.LegacyAccount,
                        GameConfigState.Address) is Dictionary configDict)
                {
                    States.Instance.SetGameConfigState(new GameConfigState(configDict));
                }
                else
                {
                    throw new FailedToInstantiateStateException<GameConfigState>();
                }

                var agentAddress = Address;
                var pledgeAddress = agentAddress.GetPledgeAddress();
                Address? patronAddress = null;
                var approved = false;
                if (await GetStateAsync(
                        ReservedAddresses.LegacyAccount,
                        pledgeAddress) is List list)
                {
                    patronAddress = list[0].ToAddress();
                    approved = list[1].ToBoolean();
                }

                States.Instance.SetPledgeStates(patronAddress, approved);
            });

            yield return new WaitUntil(() => currencyTask.IsCompleted);

            if (currencyTask.IsFaulted)
            {
                callback?.Invoke(false);
                yield break;
            }

            // 그리고 모든 액션에 대한 랜더와 언랜더를 핸들링하기 시작한다.
            BlockRenderHandler.Instance.Start(BlockRenderer);
            ActionRenderHandler.Instance.Start(ActionRenderer);

            UpdateSubscribeAddresses();
            callback?.Invoke(true);
        }

        private IEnumerator CoTxProcessor()
        {
            var i = 0;
            while (true)
            {
                yield return new WaitForSeconds(TxProcessInterval);

                if (!_queuedActions.TryDequeue(out var action))
                {
                    continue;
                }

                NcDebug.Log($"[RPCAgent] CoTxProcessor()... before MakeTransaction.({++i})");
                var task = Task.Run(async () => { await MakeTransaction(action); });
                yield return new WaitUntil(() => task.IsCompleted);
                NcDebug.Log("[RPCAgent] CoTxProcessor()... after MakeTransaction." +
                    $" task completed({task.IsCompleted})");
                if (task.IsFaulted)
                {
                    NcDebug.LogException(task.Exception);
                    // FIXME: Should restore this after fixing actual bug that MakeTransaction
                    // was throwing Exception.
                    /*Debug.LogError(
                        $"Unexpected exception occurred. re-enqueue {action} for retransmission."
                    );

                    _queuedActions.Enqueue(action);*/
                }
            }
        }

        private async Task MakeTransaction((ActionBase, Func<TxId, Task<bool>>) action)
        {
            var nonce = await GetNonceAsync();
            var gasLimit = action.Item1 is ITransferAsset or ITransferAssets ? 4L : 1L;
            var tx = NCTx.Create(
                nonce,
                PrivateKey,
                _genesis?.Hash,
                new List<IValue> { action.Item1.PlainValue },
                FungibleAssetValue.Parse(Currencies.Mead, "0.00001"),
                gasLimit
            );

            if (action.Item2 is not null)
            {
                bool callBackResult = await action.Item2(tx.Id);
                if (!callBackResult)
                {
                    NcDebug.LogError($"[RPCAgent] MakeTransaction()... callBackResult is false. TxId: {tx.Id}");
                    return;
                }
            }

            var actionsText = action.Item1.GetActionTypeAttribute().TypeIdentifier.ToString();
            Guid gameActionId = Guid.Empty;
            if (action.Item1 is GameAction gameAction)
            {
                actionsText = $"{action.Item1.GetActionTypeAttribute().TypeIdentifier}" +
                    $"({gameAction.Id.ToString()})";
                gameActionId = gameAction.Id;
            }

            NcDebug.Log("[RPCAgent] MakeTransaction()... w/" +
                $" nonce={nonce}" +
                $" PrivateKeyAddr={PrivateKey.Address.ToString()}" +
                $" GenesisBlockHash={_genesis?.Hash}" +
                $" TxId={tx.Id}" +
                $" Actions=[{actionsText}]");

            _onMakeTransactionSubject.OnNext((tx, new List<ActionBase> { action.Item1 }));
            var result = await _service.PutTransaction(tx.Serialize());
            OnTxStageEnded.OnNext(result);
            if (gameActionId != Guid.Empty)
            {
                _transactions.TryAdd(gameActionId, tx.Id);
            }
        }

        private async Task<long> GetNonceAsync()
        {
            return await _service.GetNextTxNonce(Address.ToByteArray());
        }

        public void OnRender(byte[] evaluation)
        {
            using (var cp = new MemoryStream(evaluation))
            {
                using (var decompressed = new MemoryStream())
                {
                    using (var df = new DeflateStream(cp, CompressionMode.Decompress))
                    {
                        df.CopyTo(decompressed);
                        decompressed.Seek(0, SeekOrigin.Begin);
                        var dec = decompressed.ToArray();
                        try
                        {
                            var ev = MessagePackSerializer.Deserialize<NCActionEvaluation>(dec)
                                .ToActionEvaluation();
                            ActionRenderer.ActionRenderSubject.OnNext(ev);
                        }
                        catch (Exception e)
                        {
                            NcDebug.LogError($"[RPCAgent] OnRender()... Failed to deserialize ActionEvaluation. {e}");
                        }
                    }
                }
            }
        }

        public void OnUnrender(byte[] evaluation)
        {
            // deprecated.
        }

        public void OnRenderBlock(byte[] oldTip, byte[] newTip)
        {
            UniTask.RunOnThreadPool<(long, BlockHash, HashDigest<SHA256>)>(() =>
            {
                var dict = (Dictionary)_codec.Decode(newTip);
                var newTipBlock = BlockMarshaler.UnmarshalBlock(dict);
                return (
                    newTipBlock.Index,
                    newTipBlock.Hash,
                    newTipBlock.StateRootHash);
            }).ToObservable().ObserveOnMainThread().Subscribe(tuple =>
            {
                _blockHashCache.Add(tuple.Item1, tuple.Item2);
                BlockIndex = tuple.Item1;
                BlockIndexSubject.OnNext(BlockIndex);
                BlockTipHash = tuple.Item2;
                BlockTipStateRootHash = tuple.Item3;
                BlockTipHashSubject.OnNext(BlockTipHash);
                _lastTipChangedAt = DateTimeOffset.UtcNow;

                NcDebug.Log($"[{nameof(RPCAgent)}] Render block: {BlockIndex}, {BlockTipHash.ToString()}", "RenderBlock");
                BlockRenderer.RenderBlock(null, null);
            });
        }

        private async void RegisterDisconnectEvent(IActionEvaluationHub hub)
        {
            try
            {
                await hub.WaitForDisconnect();
            }
            finally
            {
                RetryRpc();
            }
        }

        private async void RetryRpc()
        {
            OnRetryStarted.OnNext(this);
            // Dict to store tried RPC server hosts. (host, tried)
            var triedRPCHost = cachedRpcServerHosts.ToDictionary(key => key, value => false);
            NcDebug.Log($"[RPCAgent] RetryRpc()... Trying to reconnect to RPC server {RpcConnectionRetryCount} times.");
            var random = new Random();
            var retryCount = RpcConnectionRetryCount;
            while (retryCount > 0)
            {
                // Find a new RPC server host to connect that has not been tried yet.
                var newRpcServerHost = triedRPCHost
                    .Where(pair => !pair.Value).OrderBy(_ => random.Next()).FirstOrDefault().Key;
                if (newRpcServerHost is null)
                {
                    NcDebug.Log("[RPCAgent] All RPC server hosts are tried. <b>Retry failed.</b>");
                    break;
                }

                OnRetryAttempt.OnNext((this, retryCount));
                try
                {
                    await Task.Delay(5000, cancellationTokenSource.Token);
                }
                catch (Exception e)
                {
                    NcDebug.Log($"[RPCAgent] RPCAgent GameObject is disposed. <b>Retry Canceled.</b>\n{e}");
                    break;
                }

                NcDebug.Log($"[RPCAgent] Trying to connect to new RPC server host: {newRpcServerHost}:{cachedRpcServerPort}");
                try
                {
                    _channel = GrpcChannelx.ForTarget(new GrpcChannelTarget(newRpcServerHost, cachedRpcServerPort, true));
                    _hub = await StreamingHubClient.ConnectAsync<IActionEvaluationHub, IActionEvaluationHubReceiver>(_channel, this);
                    _service = MagicOnionClient.Create<IBlockChainService>(_channel, new IClientFilter[] { new ClientFilter() });
                }
                catch (RpcException re)
                {
                    NcDebug.Log($"[RPCAgent] RpcException occurred. <b>Retrying... {retryCount}/{RpcConnectionRetryCount}</b>\n{re}");
                    triedRPCHost[newRpcServerHost] = true;
                    retryCount--;
                    continue;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                try
                {
                    NcDebug.Log("[RPCAgent] Trying to join hub...");
                    await Join(true);
                    NcDebug.Log("[RPCAgent] Join complete! Registering disconnect event...");
                    RegisterDisconnectEvent(_hub);
                    UpdateSubscribeAddresses();
                    OnRetryEnded.OnNext(this);

                    return;
                }
                catch (TimeoutException toe)
                {
                    NcDebug.Log($"[RPCAgent] TimeoutException occurred. <b>Retrying... {retryCount}/{RpcConnectionRetryCount}</b>\n{toe}");
                    triedRPCHost[newRpcServerHost] = true;
                    retryCount--;
                }
                catch (AggregateException ae)
                {
                    if (ae.InnerException is RpcException re)
                    {
                        NcDebug.Log($"[RPCAgent] RpcException occurred. <b>Retrying... {retryCount}/{RpcConnectionRetryCount}</b>\n{re}");
                        triedRPCHost[newRpcServerHost] = true;
                        retryCount--;
                    }
                    else
                    {
                        NcDebug.Log($"[RPCAgent] Unexpected error occurred during rpc connection. {ae}");
                        break;
                    }
                }
                catch (ObjectDisposedException ode)
                {
                    NcDebug.Log($"[RPCAgent] ObjectDisposedException occurred. <b>Retrying... {retryCount}/{RpcConnectionRetryCount}</b>\n{ode}");
                    triedRPCHost[newRpcServerHost] = true;
                    retryCount--;
                }
                catch (Exception e)
                {
                    NcDebug.Log($"[RPCAgent] Unexpected error occurred during rpc connection. {e}");
                    break;
                }
            }

            Connected = false;
            OnDisconnected.OnNext(this);
        }

        private async Task Join(bool isRetry = false)
        {
            if (isRetry)
            {
                var joinTask = _hub.JoinAsync(Address.ToHex()).AsUniTask();
                await joinTask.Timeout(TimeSpan.FromSeconds(10));
            }
            else
            {
                await _hub.JoinAsync(Address.ToHex());
            }

            await _service.AddClient(Address.ToByteArray());
        }

        public void OnReorged(byte[] oldTip, byte[] newTip, byte[] branchpoint)
        {
            var dict = (Dictionary)DeCompressState(newTip);
            var newTipBlock = BlockMarshaler.UnmarshalBlock(dict);
            BlockIndex = newTipBlock.Index;
            BlockIndexSubject.OnNext(BlockIndex);
            BlockTipHash = new BlockHash(newTipBlock.Hash.ToByteArray());
            BlockTipHashSubject.OnNext(BlockTipHash);
            _lastTipChangedAt = DateTimeOffset.UtcNow;

            NcDebug.Log($"[{nameof(RPCAgent)}] Render reorg: {BlockIndex}, {BlockTipHash.ToString()}");
        }

        public void OnReorgEnd(byte[] oldTip, byte[] newTip, byte[] branchpoint)
        {
        }

        public void OnException(int code, string message)
        {
            var key = "ERROR_UNHANDLED";
            var errorCode = "100";
            switch (code)
            {
                case (int)RPCException.NetworkException:
                    key = "ERROR_NETWORK";
                    errorCode = "101";
                    break;

                case (int)RPCException.InvalidRenderException:
                    key = "ERROR_INVALID_RENDER";
                    errorCode = "102";
                    break;
            }

            var errorMsg = string.Format(L10nManager.Localize("UI_ERROR_RETRY_FORMAT"),
                L10nManager.Localize(key), errorCode);

            NcDebug.Log($"{message} (code: {code})");
            Lobby.Enter(true);
            Game.Game.instance.Lobby.OnLobbyEnterEnd
                .First()
                .Subscribe(_ =>
                {
                    var popup = Widget.Find<IconAndButtonSystem>();
                    popup.Show(L10nManager.Localize("UI_ERROR"),
                        errorMsg, L10nManager.Localize("UI_OK"), false);
                    popup.SetConfirmCallbackToExit();
                });
        }

        public void OnPreloadStart()
        {
            OnPreloadStarted.OnNext(this);
            NcDebug.Log($"On Preload Start");
        }

        public void OnPreloadEnd()
        {
            OnPreloadEnded.OnNext(this);
            NcDebug.Log($"On Preload End");
        }

        public void UpdateSubscribeAddresses()
        {
            // Avoid NRE in development mode
            if (PrivateKey is null)
            {
                return;
            }

            var addresses = new List<(Address, Address)> { (Addresses.Agent, Address) };

            var currentAvatarState = States.Instance.CurrentAvatarState;
            if (currentAvatarState is not null)
            {
                addresses.Add((Addresses.Avatar, (currentAvatarState.address)));
                addresses.AddRange(currentAvatarState.combinationSlotAddresses.Select(addr =>
                    (ReservedAddresses.LegacyAccount, addr)));
            }

            NcDebug.Log($"Subscribing addresses: {string.Join(", ", addresses)}");

            foreach (var address in addresses)
            {
                var game = Game.Game.instance;
                var derivedAddr = address.Item1.Derive(address.Item2.ToByteArray());
                game.CachedStateAddresses[derivedAddr] = false;
                if (!game.CachedStates.ContainsKey(derivedAddr))
                {
                    game.CachedStates.Add(derivedAddr, Null.Value);
                }
            }

            _service.SetAddressesToSubscribe(Address.ToByteArray(), addresses.Select(pair => pair.Item2.ToByteArray()));
        }

        public bool TryGetTxId(Guid actionId, out TxId txId)
        {
            return _transactions.TryGetValue(actionId, out txId);
        }

        public async UniTask<bool> IsTxStagedAsync(TxId txId)
        {
            return await _service.IsTransactionStaged(txId.ToByteArray()).ResponseAsync.AsUniTask();
        }

        /// <summary>
        /// Retrieves sheet hash values for the given address list in parallel.
        /// </summary>
        /// <param name="addressList">List of addresses to fetch hash values for</param>
        /// <returns>Dictionary mapping addresses to their corresponding hash values</returns>
        /// <remarks>
        /// - Processes addresses in chunks of 24 in parallel
        /// - Uses ConcurrentDictionary to ensure thread safety
        /// - Includes Stopwatch for performance measurement
        /// </remarks>
        public async Task<Dictionary<Address, byte[]>> GetSheetsHash(IEnumerable<Address> addressList)
        {
            var sw = new Stopwatch();
            sw.Start();
            var cd = new ConcurrentDictionary<byte[], byte[]>();
            var chunks = addressList
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / 24)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
            await chunks
                .AsParallel()
                .ParallelForEachAsync(async list =>
                {
                    var raw =
                        await _service.GetSheetsHash(
                            BlockTipStateRootHash.ToByteArray(),
                            list.Select(a => a.ToByteArray()));
                    await raw.ParallelForEachAsync(async pair =>
                    {
                        cd.TryAdd(pair.Key, pair.Value);
                        var size = Buffer.ByteLength(pair.Value);
                        await Task.CompletedTask;
                    });
                });
            sw.Stop();
            sw.Restart();
            var result = new Dictionary<Address, byte[]>();
            NcDebug.Log($"[{nameof(RPCAgent)}] GetSheetsHash()... {sw.Elapsed}/{cd.Count}");
            foreach (var kv in cd)
            {
                result[new Address(kv.Key)] = kv.Value;
            }

            sw.Stop();
            return result;
        }

        private async UniTask<BlockHash?> GetBlockHashAsync(long? blockIndex)
        {
            return blockIndex.HasValue
                ? _blockHashCache.TryGetBlockHash(blockIndex.Value, out var outBlockHash)
                    ? outBlockHash
                    : DeCompressState(await _service.GetBlockHash(blockIndex.Value)) is { } rawBlockHash
                        ? new BlockHash(rawBlockHash)
                        : (BlockHash?)null
                : BlockTipHash;
        }

        private IValue DeCompressState(byte[] compressed)
        {
            using (var cp = new MemoryStream(compressed))
            {
                using (var decompressed = new MemoryStream())
                {
                    using (var df = new DeflateStream(cp, CompressionMode.Decompress))
                    {
                        df.CopyTo(decompressed);
                        decompressed.Seek(0, SeekOrigin.Begin);
                        var dec = decompressed.ToArray();
                        return _codec.Decode(dec);
                    }
                }
            }
        }
    }
}
