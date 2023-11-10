using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Bencodex;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Grpc.Core;
using Ionic.Zlib;
using Lib9c;
using Lib9c.Renderers;
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
using Nekoyume.Blockchain.Policy;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Stake;
using Nekoyume.Model.State;
using Nekoyume.Shared.Hubs;
using Nekoyume.Shared.Services;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI;
using NineChronicles.RPC.Shared.Exceptions;
using UnityEngine;
using Channel = Grpc.Core.Channel;
using Logger = Serilog.Core.Logger;
using NCTx = Libplanet.Types.Tx.Transaction;

namespace Nekoyume.Blockchain
{
    using UniRx;

    public class RPCAgent : MonoBehaviour, IAgent, IActionEvaluationHubReceiver
    {
        private const int RpcConnectionRetryCount = 6;
        private const float TxProcessInterval = 1.0f;
        private readonly ConcurrentQueue<ActionBase> _queuedActions = new ConcurrentQueue<ActionBase>();

        private readonly TransactionMap _transactions = new TransactionMap(20);

        private GrpcChannelx _channel;

        private IActionEvaluationHub _hub;

        private IBlockChainService _service;

        private Codec _codec = new Codec();

        private Block _genesis;

        private DateTimeOffset _lastTipChangedAt;

        public BlockRenderer BlockRenderer { get; } = new BlockRenderer();

        public ActionRenderer ActionRenderer { get; } = new ActionRenderer();

        public Subject<long> BlockIndexSubject { get; } = new Subject<long>();

        public Subject<BlockHash> BlockTipHashSubject { get; } = new Subject<BlockHash>();

        public long BlockIndex { get; private set; }

        public PrivateKey PrivateKey { get; private set; }

        public Address Address => PrivateKey.PublicKey.ToAddress();

        public bool Connected { get; private set; }

        public readonly Subject<RPCAgent> OnDisconnected = new Subject<RPCAgent>();

        public readonly Subject<RPCAgent> OnRetryStarted = new Subject<RPCAgent>();

        public readonly Subject<RPCAgent> OnRetryEnded = new Subject<RPCAgent>();

        public readonly Subject<RPCAgent> OnPreloadStarted = new Subject<RPCAgent>();

        public readonly Subject<RPCAgent> OnPreloadEnded = new Subject<RPCAgent>();

        public readonly Subject<(RPCAgent, int retryCount)> OnRetryAttempt = new Subject<(RPCAgent, int)>();

        public BlockHash BlockTipHash { get; private set; }

        private readonly Subject<(NCTx tx, List<ActionBase> actions)> _onMakeTransactionSubject =
            new Subject<(NCTx tx, List<ActionBase> actions)>();

        public IObservable<(NCTx tx, List<ActionBase> actions)> OnMakeTransaction => _onMakeTransactionSubject;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private readonly BlockHashCache _blockHashCache = new(100);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void OnRuntimeInitialize()
        {
            // Initialize gRPC channel provider when the application is loaded.
            GrpcChannelProviderHost.Initialize(new LoggingGrpcChannelProvider(
                new DefaultGrpcChannelProvider(new[]
                {
                    new ChannelOption("grpc.max_receive_message_length", -1)
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
            Debug.Log($"[RPCAgent] Start initialization: {options.RpcServerHost}:{options.RpcServerPort}");
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

            IEnumerator GetTip()
            {
                var getTipTask = Task.Run(async () => await _service.GetTip());
                yield return new WaitUntil(() => getTipTask.IsCompleted);
                OnRenderBlock(null, getTipTask.Result);
            }

            var getTipCoroutine = StartCoroutine(GetTip());

            if (_genesis == null)
            {
                // Android Mono only support arm7(32bit) backend in unity engine.
                bool architecture_is_32bit = ! Environment.Is64BitProcess;
                bool is_Android = Application.platform == RuntimePlatform.Android;
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
            }

            yield return getTipCoroutine;
            RegisterDisconnectEvent(_hub);
            StartCoroutine(CoTxProcessor());
            StartCoroutine(CoJoin(callback));
            Debug.Log($"[RPCAgent] Finish initialization");
        }

        public IValue GetState(Address address)
        {
            var raw = _service.GetState(
                address.ToByteArray(),
                BlockTipHash.ToByteArray()
            ).ResponseAsync.Result;
            return _codec.Decode(raw);
        }

        public IValue GetState(Address address, HashDigest<SHA256> stateRootHash)
        {
            var raw = _service.GetStateBySrh(
                address.ToByteArray(),
                stateRootHash.ToByteArray()
            ).ResponseAsync.Result;
            return _codec.Decode(raw);
        }

        public async Task<IValue> GetStateAsync(Address address, long? blockIndex = null)
        {
            var game = Game.Game.instance;
            // Check state & cached because force update state after rpc disconnected.
            if (!blockIndex.HasValue &&
                game.CachedStateAddresses.TryGetValue(address, out var cached) &&
                cached &&
                game.CachedStates.TryGetValue(address, out var value) &&
                value is not Null)
            {
                return value;
            }

            var blockHash = await GetBlockHashAsync(blockIndex);
            if (!blockHash.HasValue)
            {
                Debug.LogError($"Failed to get block hash from block index: {blockIndex}");
                return null;
            }

            return await GetStateAsync(address, blockHash.Value);
        }

        public async Task<IValue> GetStateAsync(Address address, BlockHash blockHash)
        {
            var bytes = await _service.GetState(address.ToByteArray(), blockHash.ToByteArray());
            var decoded = _codec.Decode(bytes);
            var game = Game.Game.instance;
            if (game.CachedStateAddresses.ContainsKey(address))
            {
                game.CachedStateAddresses[address] = true;
            }

            if (game.CachedStates.ContainsKey(address))
            {
                game.CachedStates.AddOrUpdate(address, decoded);
            }

            return decoded;
        }

        public async Task<IValue> GetStateAsync(Address address, HashDigest<SHA256> stateRootHash)
        {
            var bytes = await _service.GetStateBySrh(address.ToByteArray(), stateRootHash.ToByteArray());
            var decoded = _codec.Decode(bytes);
            var game = Game.Game.instance;
            if (game.CachedStateAddresses.ContainsKey(address))
            {
                game.CachedStateAddresses[address] = true;
            }

            if (game.CachedStates.ContainsKey(address))
            {
                game.CachedStates.AddOrUpdate(address, decoded);
            }

            return decoded;
        }

        public FungibleAssetValue GetBalance(Address addr, Currency currency)
        {
            return GetBalanceAsync(addr, currency).Result;
        }

        public async Task<FungibleAssetValue> GetBalanceAsync(
            Address addr,
            Currency currency,
            long? blockIndex = null)
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
                Debug.LogError($"Failed to get block hash from block index: {blockIndex}");
                return 0 * currency;
            }

            var balance = await GetBalanceAsync(addr, currency, blockHash.Value)
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
            Address address,
            Currency currency,
            BlockHash blockHash)
        {
            var raw = await _service.GetBalance(
                address.ToByteArray(),
                _codec.Encode(currency.Serialize()),
                BlockTipHash.ToByteArray());
            var serialized = (List) _codec.Decode(raw);
            return FungibleAssetValue.FromRawValue(
                new Currency(serialized.ElementAt(0)),
                serialized.ElementAt(1).ToBigInteger());
        }

        public async Task<FungibleAssetValue> GetBalanceAsync(
            Address address,
            Currency currency,
            HashDigest<SHA256> stateRootHash)
        {
            var raw = await _service.GetBalanceBySrh(
                address.ToByteArray(),
                _codec.Encode(currency.Serialize()),
                stateRootHash.ToByteArray());
            var serialized = (List) _codec.Decode(raw);
            return FungibleAssetValue.FromRawValue(
                new Currency(serialized.ElementAt(0)),
                serialized.ElementAt(1).ToBigInteger());
        }

        public async Task<Dictionary<Address, AvatarState>> GetAvatarStatesAsync(
            IEnumerable<Address> addressList,
            long? blockIndex = null)
        {
            var blockHash = await GetBlockHashAsync(blockIndex);
            if (!blockHash.HasValue)
            {
                Debug.LogError($"Failed to get block hash from block index: {blockIndex}");
                return null;
            }

            Dictionary<byte[], byte[]> raw = await _service.GetAvatarStates(
                addressList.Select(a => a.ToByteArray()),
                blockHash.Value.ToByteArray());
            var result = new Dictionary<Address, AvatarState>();
            foreach (var kv in raw)
            {
                result[new Address(kv.Key)] = new AvatarState((Dictionary)_codec.Decode(kv.Value));
            }
            return result;
        }

        public async Task<Dictionary<Address, AvatarState>> GetAvatarStatesAsync(
            IEnumerable<Address> addressList,
            HashDigest<SHA256> stateRootHash)
        {
            Dictionary<byte[], byte[]> raw = await _service.GetAvatarStatesBySrh(
                addressList.Select(a => a.ToByteArray()),
                stateRootHash.ToByteArray());
            var result = new Dictionary<Address, AvatarState>();
            foreach (var kv in raw)
            {
                result[new Address(kv.Key)] = new AvatarState((Dictionary)_codec.Decode(kv.Value));
            }
            return result;
        }

        public async Task<Dictionary<Address, IValue>> GetStateBulkAsync(IEnumerable<Address> addressList)
        {
            Dictionary<byte[], byte[]> raw =
                await _service.GetStateBulk(addressList.Select(a => a.ToByteArray()),
                    BlockTipHash.ToByteArray());
            var result = new Dictionary<Address, IValue>();
            foreach (var kv in raw)
            {
                result[new Address(kv.Key)] = _codec.Decode(kv.Value);
            }
            return result;
        }

        public async Task<Dictionary<Address, IValue>> GetStateBulkAsync(
            IEnumerable<Address> addressList,
            HashDigest<SHA256> stateRootHash)
        {
            Dictionary<byte[], byte[]> raw =
                await _service.GetStateBulkBySrh(
                    addressList.Select(a => a.ToByteArray()),
                    stateRootHash.ToByteArray());
            var result = new Dictionary<Address, IValue>();
            foreach (var kv in raw)
            {
                result[new Address(kv.Key)] = _codec.Decode(kv.Value);
            }
            return result;
        }

        public void SendException(Exception exc)
        {
        }

        public void EnqueueAction(ActionBase actionBase)
        {
            _queuedActions.Enqueue(actionBase);
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
                .Subscribe(_ => Analyzer.Instance?.Track("Unity/RPC Retry Connect Started",GetPlayerAddressForLogging()))
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
                .Subscribe(tuple =>
                {
                    Debug.Log($"Retry rpc connection. (remain count: {tuple.retryCount})");
                    var tryCount = RpcConnectionRetryCount - tuple.retryCount;
                    if (tryCount > 0)
                    {
                        Widget.Find<DimmedLoadingScreen>()?.Show();
                    }
                })
                .AddTo(_disposables);
            Game.Event.OnUpdateAddresses.AddListener(UpdateSubscribeAddresses);
        }

        private async void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
            _onMakeTransactionSubject.Dispose();

            BlockRenderHandler.Instance.Stop();
            ActionRenderHandler.Instance.Stop();

            StopAllCoroutines();
            if (!(_hub is null))
            {
                await _hub.DisposeAsync();
            }
            if (!(_channel is null))
            {
                await _channel?.ShutdownAsync();
            }
        }

        #endregion

        private IEnumerator CoJoin(Action<bool> callback)
        {
            Task t = Task.Run(async () =>
            {
                await Join();
            });

            yield return new WaitUntil(() => t.IsCompleted);

            if (t.IsFaulted)
            {
                callback?.Invoke(false);
                yield break;
            }

            Connected = true;

            // 에이전트의 상태를 한 번 동기화 한다.
            Task currencyTask = Task.Run(async () =>
            {
                var goldCurrency = new GoldCurrencyState(
                    (Dictionary)await GetStateAsync(GoldCurrencyState.Address)
                ).Currency;
                ActionRenderHandler.Instance.GoldCurrency = goldCurrency;

                await States.Instance.SetAgentStateAsync(
                    await GetStateAsync(Address) is Dictionary agentDict
                        ? new AgentState(agentDict)
                        : new AgentState(Address));
                States.Instance.SetGoldBalanceState(
                    new GoldBalanceState(
                        Address,
                        await GetBalanceAsync(Address, goldCurrency)));
                States.Instance.SetCrystalBalance(
                    await GetBalanceAsync(Address, Currencies.Crystal));

                if (await GetStateAsync(GameConfigState.Address) is Dictionary configDict)
                {
                    States.Instance.SetGameConfigState(new GameConfigState(configDict));
                }
                else
                {
                    throw new FailedToInstantiateStateException<GameConfigState>();
                }

                // NOTE: Initialize staking states after setting GameConfigState.
                var stakeAddr = StakeStateV2.DeriveAddress(Address);
                if (await GetStateAsync(stakeAddr) is { } serializedStakeState)
                {
                    if (!StakeStateUtilsForClient.TryMigrate(
                            serializedStakeState,
                            States.Instance.GameConfigState,
                            out var stakeStateV2))
                    {
                        States.Instance.SetStakeState(null, null, 0, null, null);
                    }
                    else
                    {
                        var balance = new FungibleAssetValue(goldCurrency);
                        var level = 0;
                        var stakeRegularFixedRewardSheet = new StakeRegularFixedRewardSheet();
                        var stakeRegularRewardSheet = new StakeRegularRewardSheet();
                        try
                        {
                            balance = await GetBalanceAsync(stakeAddr, goldCurrency);
                            var sheetAddrArr = new[]
                            {
                                Addresses.GetSheetAddress(
                                    stakeStateV2.Contract.StakeRegularFixedRewardSheetTableName),
                                Addresses.GetSheetAddress(
                                    stakeStateV2.Contract.StakeRegularRewardSheetTableName),
                            };
                            var sheetStates = await GetStateBulkAsync(sheetAddrArr);
                            stakeRegularFixedRewardSheet.Set(
                                sheetStates[sheetAddrArr[0]].ToDotnetString());
                            stakeRegularRewardSheet.Set(
                                sheetStates[sheetAddrArr[1]].ToDotnetString());
                            level = stakeRegularFixedRewardSheet.FindLevelByStakedAmount(
                                Address,
                                balance);
                        }
                        catch
                        {
                            // ignored
                        }

                        States.Instance.SetStakeState(
                            stakeStateV2,
                            new GoldBalanceState(stakeAddr, balance),
                            level,
                            stakeRegularFixedRewardSheet,
                            stakeRegularRewardSheet);
                    }
                }

                var agentAddress = Address;
                var pledgeAddress = agentAddress.GetPledgeAddress();
                Address? patronAddress = null;
                var approved = false;
                if (await GetStateAsync(pledgeAddress) is List list)
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
            int i = 0;
            while (true)
            {
                yield return new WaitForSeconds(TxProcessInterval);

                if (!_queuedActions.TryDequeue(out ActionBase action))
                {
                    continue;
                }
                Debug.Log($"[RPCAgent] CoTxProcessor()... before MakeTransaction.({++i})");
                Task task = Task.Run(async () =>
                {
                    await MakeTransaction(new List<ActionBase> { action });
                });
                yield return new WaitUntil(() => task.IsCompleted);
                Debug.Log("[RPCAgent] CoTxProcessor()... after MakeTransaction." +
                          $" task completed({task.IsCompleted})");
                if (task.IsFaulted)
                {
                    Debug.LogException(task.Exception);
                    // FIXME: Should restore this after fixing actual bug that MakeTransaction
                    // was throwing Exception.
                    /*Debug.LogError(
                        $"Unexpected exception occurred. re-enqueue {action} for retransmission."
                    );

                    _queuedActions.Enqueue(action);*/
                }
            }
        }

        private async Task MakeTransaction(List<ActionBase> actions)
        {
            var nonce = await GetNonceAsync();
            var gasLimit = actions.Any(a => a is ITransferAsset or ITransferAssets) ? 4L : 1L;
            var tx = NCTx.Create(
                nonce: nonce,
                privateKey: PrivateKey,
                genesisHash: _genesis?.Hash,
                actions: actions.Select(action => action.PlainValue),
                updatedAddresses: actions.CalculateUpdateAddresses(),
                maxGasPrice: Currencies.Mead * 1,
                gasLimit: gasLimit
            );

            var actionsText = string.Join(", ", actions.Select(action =>
            {
                if (action is GameAction gameAction)
                {
                    return $"{action.GetActionTypeAttribute().TypeIdentifier}" +
                           $"({gameAction.Id.ToString()})";
                }

                return action.GetActionTypeAttribute().TypeIdentifier.ToString();
            }));
            Debug.Log("[RPCAgent] MakeTransaction()... w/" +
                      $" nonce={nonce}" +
                      $" PrivateKeyAddr={PrivateKey.ToAddress().ToString()}" +
                      $" GenesisBlockHash={_genesis?.Hash}" +
                      $" TxId={tx.Id}" +
                      $" Actions=[{actionsText}]");

            _onMakeTransactionSubject.OnNext((tx, actions));
            await _service.PutTransaction(tx.Serialize());
            foreach (var action in actions)
            {
                if (action is GameAction gameAction)
                {
                    _transactions.TryAdd(gameAction.Id, tx.Id);
                }
            }
        }

        private async Task<long> GetNonceAsync()
        {
            return await _service.GetNextTxNonce(Address.ToByteArray());
        }

        public void OnRender(byte[] evaluation)
        {
            using (var cp = new MemoryStream(evaluation))
            using (var decompressed = new MemoryStream())
            using (var df = new DeflateStream(cp, CompressionMode.Decompress))
            {
                df.CopyTo(decompressed);
                decompressed.Seek(0, SeekOrigin.Begin);
                var dec = decompressed.ToArray();
                var ev = MessagePackSerializer.Deserialize<NCActionEvaluation>(dec)
                    .ToActionEvaluation();
                ActionRenderer.ActionRenderSubject.OnNext(ev);
            }
        }

        public void OnUnrender(byte[] evaluation)
        {
            // deprecated.
        }

        public void OnRenderBlock(byte[] oldTip, byte[] newTip)
        {
            var dict = (Bencodex.Types.Dictionary)_codec.Decode(newTip);
            var newTipBlock = BlockMarshaler.UnmarshalBlock(dict);
            var blockIndex = newTipBlock.Index;
            var blockHash = new BlockHash(newTipBlock.Hash.ToByteArray());
            _blockHashCache.Add(blockIndex, blockHash);
            BlockIndex = blockIndex;
            BlockIndexSubject.OnNext(BlockIndex);
            BlockTipHash = blockHash;
            BlockTipHashSubject.OnNext(BlockTipHash);
            _lastTipChangedAt = DateTimeOffset.UtcNow;

            Debug.Log($"[{nameof(RPCAgent)}] Render block: {BlockIndex}, {BlockTipHash.ToString()}");
            BlockRenderer.RenderBlock(null, null);
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
            var retryCount = RpcConnectionRetryCount;
            while (retryCount > 0)
            {
                OnRetryAttempt.OnNext((this, retryCount));
                await Task.Delay(5000);
                try
                {
                    _hub = StreamingHubClient.Connect<IActionEvaluationHub, IActionEvaluationHubReceiver>(_channel, this);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                try
                {
                    Debug.Log($"Trying to join hub...");
                    await Join(true);
                    Debug.Log($"Join complete! Registering disconnect event...");
                    RegisterDisconnectEvent(_hub);
                    UpdateSubscribeAddresses();
                    OnRetryEnded.OnNext(this);
                    return;
                }
                catch (TimeoutException toe)
                {
                    Debug.LogWarning($"TimeoutException occurred. Retrying... {retryCount}\n{toe}");
                    retryCount--;
                }
                catch (AggregateException ae)
                {
                    if (ae.InnerException is RpcException re)
                    {
                        Debug.LogWarning($"RpcException occurred. Retrying... {retryCount}\n{re}");
                        retryCount--;
                    }
                    else
                    {
                        Debug.LogWarning($"Unexpected error occurred during rpc connection. {ae}");
                        break;
                    }
                }
                catch (ObjectDisposedException ode)
                {
                    Debug.LogWarning($"ObjectDisposedException occurred. Retrying... {retryCount}\n{ode}");
                    retryCount--;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Unexpected error occurred during rpc connection. {e}");
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
            var dict = (Bencodex.Types.Dictionary)_codec.Decode(newTip);
            Block newTipBlock = BlockMarshaler.UnmarshalBlock(dict);
            BlockIndex = newTipBlock.Index;
            BlockIndexSubject.OnNext(BlockIndex);
            BlockTipHash = new BlockHash(newTipBlock.Hash.ToByteArray());
            BlockTipHashSubject.OnNext(BlockTipHash);
            _lastTipChangedAt = DateTimeOffset.UtcNow;

            Debug.Log($"[{nameof(RPCAgent)}] Render reorg: {BlockIndex}, {BlockTipHash.ToString()}");
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

            Debug.Log($"{message} (code: {code})");
            Game.Event.OnRoomEnter.Invoke(true);
            Game.Game.instance.Stage.OnRoomEnterEnd
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
            Debug.Log($"On Preload Start");
        }

        public void OnPreloadEnd()
        {
            OnPreloadEnded.OnNext(this);
            Debug.Log($"On Preload End");
        }

        public void UpdateSubscribeAddresses()
        {
            // Avoid NRE in development mode
            if (PrivateKey is null)
            {
                return;
            }

            var addresses = new List<Address> { Address };

            var currentAvatarState = States.Instance.CurrentAvatarState;
            if (!(currentAvatarState is null))
            {
                addresses.Add(currentAvatarState.address);
                var slotAddresses = currentAvatarState.combinationSlotAddresses.ToArray();
                addresses.AddRange(slotAddresses);
            }

            Debug.Log($"Subscribing addresses: {string.Join(", ", addresses)}");

            foreach (var address in addresses)
            {
                var game = Game.Game.instance;
                game.CachedStateAddresses[address] = false;
                if (!game.CachedStates.ContainsKey(address))
                {
                    game.CachedStates.Add(address, Null.Value);
                }
            }

            _service.SetAddressesToSubscribe(Address.ToByteArray(), addresses.Select(addr => addr.ToByteArray()));
        }

        public bool TryGetTxId(Guid actionId, out TxId txId) =>
            _transactions.TryGetValue(actionId, out txId);

        public async UniTask<bool> IsTxStagedAsync(TxId txId) =>
            await _service.IsTransactionStaged(txId.ToByteArray()).ResponseAsync.AsUniTask();

        private async UniTask<BlockHash?> GetBlockHashAsync(long? blockIndex)
        {
            return blockIndex.HasValue
                ? _blockHashCache.TryGetBlockHash(blockIndex.Value, out var outBlockHash)
                    ? outBlockHash
                    : _codec.Decode(await _service.GetBlockHash(blockIndex.Value)) is { } rawBlockHash
                        ? new BlockHash(rawBlockHash)
                        : (BlockHash?)null
                : BlockTipHash;
        }
    }
}
