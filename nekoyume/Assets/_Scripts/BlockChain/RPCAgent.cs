using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bencodex;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Grpc.Core;
using Ionic.Zlib;
using Lib9c.Renderer;
using Libplanet;
using Libplanet.Assets;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Tx;
using MagicOnion.Client;
using MessagePack;
using Nekoyume.Action;
using Nekoyume.BlockChain.Policy;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.Shared.Hubs;
using Nekoyume.Shared.Services;
using Nekoyume.State;
using Nekoyume.UI;
using NineChronicles.RPC.Shared.Exceptions;
using UnityEngine;
using Channel = Grpc.Core.Channel;
using Logger = Serilog.Core.Logger;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;
using NCTx = Libplanet.Tx.Transaction<Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>>;

namespace Nekoyume.BlockChain
{
    using System.Threading;
    using UniRx;

    public class RPCAgent : MonoBehaviour, IAgent, IActionEvaluationHubReceiver
    {
        private const int RpcConnectionRetryCount = 10;
        private const float TxProcessInterval = 1.0f;
        private readonly ConcurrentQueue<NCAction> _queuedActions = new ConcurrentQueue<NCAction>();

        private readonly TransactionMap _transactions = new TransactionMap(20);

        private Channel _channel;

        private IActionEvaluationHub _hub;

        private IBlockChainService _service;

        private Codec _codec = new Codec();

        private Block<NCAction> _genesis;

        private DateTimeOffset _lastTipChangedAt;

        // Rendering logs will be recorded in NineChronicles.Standalone
        public BlockPolicySource BlockPolicySource { get; } = new BlockPolicySource(Logger.None);

        public BlockRenderer BlockRenderer => BlockPolicySource.BlockRenderer;

        public ActionRenderer ActionRenderer => BlockPolicySource.ActionRenderer;

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

        public int AppProtocolVersion { get; private set; }

        public BlockHash BlockTipHash { get; private set; }

        private readonly Subject<(NCTx tx, List<NCAction> actions)> _onMakeTransactionSubject =
                new Subject<(NCTx tx, List<NCAction> actions)>();

        public IObservable<(NCTx tx, List<NCAction> actions)> OnMakeTransaction => _onMakeTransactionSubject;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public IEnumerator Initialize(
            CommandLineOptions options,
            PrivateKey privateKey,
            Action<bool> callback)
        {
            PrivateKey = privateKey;

            _channel = new Channel(
                options.RpcServerHost,
                options.RpcServerPort,
                ChannelCredentials.Insecure,
                new[]
                {
                    new ChannelOption("grpc.max_receive_message_length", -1)
                }
            );
            _lastTipChangedAt = DateTimeOffset.UtcNow;
            _hub = StreamingHubClient.Connect<IActionEvaluationHub, IActionEvaluationHubReceiver>(_channel, this);
            _service = MagicOnionClient.Create<IBlockChainService>(_channel, new IClientFilter[]
            {
                new ClientFilter()
            }).WithCancellationToken(_channel.ShutdownToken);
            var getTipTask = Task.Run(async () => await _service.GetTip());
            yield return new WaitUntil(() => getTipTask.IsCompleted);
            OnRenderBlock(null, getTipTask.Result);
            yield return null;
            var task = Task.Run(async () =>
            {
                _genesis = await BlockManager.ImportBlockAsync(options.GenesisBlockPath ?? BlockManager.GenesisBlockPath);
            });
            yield return new WaitUntil(() => task.IsCompleted);
            var appProtocolVersion = options.AppProtocolVersion is null
                ? default
                : Libplanet.Net.AppProtocolVersion.FromToken(options.AppProtocolVersion);
            AppProtocolVersion = appProtocolVersion.Version;

            RegisterDisconnectEvent(_hub);
            StartCoroutine(CoTxProcessor());
            StartCoroutine(CoJoin(callback));
        }

        public IValue GetState(Address address)
        {
            byte[] raw = _service.GetState(address.ToByteArray(), BlockTipHash.ToByteArray()).ResponseAsync.Result;
            return _codec.Decode(raw);
        }

        public async Task<IValue> GetStateAsync(Address address)
        {
            // Check state & cached because force update state after rpc disconnected.
            if (Game.Game.instance.CachedAddresses.TryGetValue(address, out bool cached) && cached &&
                Game.Game.instance.CachedStates.TryGetValue(address, out IValue value) && !(value is Null))
            {
                await Task.CompletedTask;
                return value;
            }
            byte[] raw = await _service.GetState(address.ToByteArray(), BlockTipHash.ToByteArray());
            IValue result = _codec.Decode(raw);
            if (Game.Game.instance.CachedAddresses.ContainsKey(address))
            {
                Game.Game.instance.CachedAddresses[address] = true;
            }
            if (Game.Game.instance.CachedStates.ContainsKey(address))
            {
                Game.Game.instance.CachedStates.AddOrUpdate(address, result);
            }
            return result;
        }

        public FungibleAssetValue GetBalance(Address address, Currency currency)
        {
            // FIXME: `CurrencyExtension.Serialize()` should be changed to `Currency.Serialize()`.
            var result = _service.GetBalance(
                address.ToByteArray(),
                _codec.Encode(CurrencyExtensions.Serialize(currency)),
                BlockTipHash.ToByteArray()
            );
            byte[] raw = result.ResponseAsync.Result;
            var serialized = (Bencodex.Types.List) _codec.Decode(raw);
            return FungibleAssetValue.FromRawValue(
                new Currency(serialized.ElementAt(0)),
                serialized.ElementAt(1).ToBigInteger());
        }

        public async Task<FungibleAssetValue> GetBalanceAsync(Address address, Currency currency)
        {
            if (Game.Game.instance.CachedBalance.TryGetValue(address, out FungibleAssetValue value) &&
                !value.Equals(default) && Game.Game.instance.CachedAddresses.TryGetValue(address, out bool cached) &&
                cached)
            {
                await Task.CompletedTask;
                return value;
            }
            // FIXME: `CurrencyExtension.Serialize()` should be changed to `Currency.Serialize()`.
            byte[] raw = await _service.GetBalance(
                address.ToByteArray(),
                _codec.Encode(CurrencyExtensions.Serialize(currency)),
                BlockTipHash.ToByteArray()
            );
            var serialized = (Bencodex.Types.List) _codec.Decode(raw);
            var balance = FungibleAssetValue.FromRawValue(
                new Currency(serialized.ElementAt(0)),
                serialized.ElementAt(1).ToBigInteger());
            if (address.Equals(Address))
            {
                Game.Game.instance.CachedBalance[Address] = balance;
            }

            return balance;
        }

        public async Task<Dictionary<Address, AvatarState>> GetAvatarStates(IEnumerable<Address> addressList)
        {
            Dictionary<byte[], byte[]> raw =
                await _service.GetAvatarStates(addressList.Select(a => a.ToByteArray()),
                    BlockTipHash.ToByteArray());
            var result = new Dictionary<Address, AvatarState>();
            foreach (var kv in raw)
            {
                result[new Address(kv.Key)] = new AvatarState((Dictionary)_codec.Decode(kv.Value));
            }
            return result;
        }

        public async Task<Dictionary<Address, IValue>> GetStateBulk(IEnumerable<Address> addressList)
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

        public void SendException(Exception exc)
        {
        }

        public void EnqueueAction(GameAction action)
        {
            _queuedActions.Enqueue(action);
        }

        #region Mono

        private void Awake()
        {
            OnDisconnected
                .ObserveOnMainThread()
                .Subscribe(_ => Analyzer.Instance.Track("Unity/RPC Disconnected"))
                .AddTo(_disposables);
            OnRetryStarted
                .ObserveOnMainThread()
                .Subscribe(_ => Analyzer.Instance.Track("Unity/RPC Retry Connect Started"))
                .AddTo(_disposables);
            OnRetryEnded
                .ObserveOnMainThread()
                .Subscribe(_ => Analyzer.Instance.Track("Unity/RPC Retry Connect Ended"))
                .AddTo(_disposables);
            OnPreloadStarted
                .ObserveOnMainThread()
                .Subscribe(_ => Analyzer.Instance.Track("Unity/RPC Preload Started"))
                .AddTo(_disposables);
            OnPreloadEnded
                .ObserveOnMainThread()
                .Subscribe(_ => Analyzer.Instance.Track("Unity/RPC Preload Ended"))
                .AddTo(_disposables);
            OnRetryAttempt
                .ObserveOnMainThread()
                .Subscribe(tuple =>
                {
                    Debug.Log($"Retry rpc connection. (count: {tuple.retryCount})");
                    var message =
                        L10nManager.Localize("UI_RETRYING_RPC_CONNECTION_FORMAT",
                        RpcConnectionRetryCount - tuple.retryCount + 1,
                        RpcConnectionRetryCount);
                    Widget.Find<DimmedLoadingScreen>()?.Show(message, true);
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
            ActionUnrenderHandler.Instance.Stop();

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
                var state = await GetStateAsync(GoldCurrencyState.Address);
                Currency goldCurrency = new GoldCurrencyState(
                    (Dictionary)state
                ).Currency;

                await States.Instance.SetAgentStateAsync(
                    await GetStateAsync(Address) is Bencodex.Types.Dictionary agentDict
                        ? new AgentState(agentDict)
                        : new AgentState(Address));
                States.Instance.SetGoldBalanceState(
                    new GoldBalanceState(Address, await GetBalanceAsync(Address, goldCurrency)));
                States.Instance.SetCrystalBalance(
                    await GetBalanceAsync(Address, CrystalCalculator.CRYSTAL));

                // 상점의 상태를 한 번 동기화 한다.

                if (await GetStateAsync(GameConfigState.Address) is Dictionary configDict)
                {
                    States.Instance.SetGameConfigState(new GameConfigState(configDict));
                }
                else
                {
                    throw new FailedToInstantiateStateException<GameConfigState>();
                }

                // FIXME: BlockIndex may not initialized.
                var weeklyArenaState = await ArenaHelperOld.GetThisWeekStateAsync(BlockIndex);
                if (weeklyArenaState is null)
                {
                    throw new FailedToInstantiateStateException<WeeklyArenaState>();
                }

                States.Instance.SetWeeklyArenaState(weeklyArenaState);

                ActionRenderHandler.Instance.GoldCurrency = goldCurrency;

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
            ActionUnrenderHandler.Instance.Start(ActionRenderer);

            UpdateSubscribeAddresses();
            callback?.Invoke(true);
        }

        private IEnumerator CoTxProcessor()
        {
            while (true)
            {
                yield return new WaitForSeconds(TxProcessInterval);

                if (!_queuedActions.TryDequeue(out NCAction action))
                {
                    continue;
                }

                Task task = Task.Run(async () =>
                {
                    await MakeTransaction(new List<NCAction> { action });
                });
                yield return new WaitUntil(() => task.IsCompleted);

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

        private async Task MakeTransaction(List<NCAction> actions)
        {
            var nonce = await GetNonceAsync();
            var tx = NCTx.Create(
                nonce,
                PrivateKey,
                _genesis?.Hash,
                actions
            );
            _onMakeTransactionSubject.OnNext((tx, actions));
            await _service.PutTransaction(tx.Serialize(true));

            foreach (var action in actions)
            {
                var ga = (GameAction) action.InnerAction;
                _transactions.TryAdd(ga.Id, tx.Id);
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
            using (var cp = new MemoryStream(evaluation))
            using (var decompressed = new MemoryStream())
            using (var df = new DeflateStream(cp, CompressionMode.Decompress))
            {
                df.CopyTo(decompressed);
                decompressed.Seek(0, SeekOrigin.Begin);
                var dec = decompressed.ToArray();
                var ev = MessagePackSerializer.Deserialize<NCActionEvaluation>(dec)
                    .ToActionEvaluation();
                ActionRenderer.ActionUnrenderSubject.OnNext(ev);
            }
        }

        public void OnRenderBlock(byte[] oldTip, byte[] newTip)
        {
            var dict = (Bencodex.Types.Dictionary)_codec.Decode(newTip);
            HashAlgorithmGetter hashAlgorithmGetter = Game.Game.instance.Agent.BlockPolicySource
                .GetPolicy()
                .GetHashAlgorithm;
            Block<NCAction> newTipBlock = BlockMarshaler.UnmarshalBlock<NCAction>(hashAlgorithmGetter, dict);
            BlockIndex = newTipBlock.Index;
            BlockIndexSubject.OnNext(BlockIndex);
            BlockTipHash = new BlockHash(newTipBlock.Hash.ToByteArray());
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
                catch (RpcException re)
                {
                    Debug.LogWarning($"RpcException occurred. Retrying... {retryCount}\n{re}");
                    retryCount--;
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
            HashAlgorithmGetter hashAlgorithmGetter = Game.Game.instance.Agent.BlockPolicySource
                .GetPolicy()
                .GetHashAlgorithm;
            Block<NCAction> newTipBlock = BlockMarshaler.UnmarshalBlock<NCAction>(hashAlgorithmGetter, dict);
            BlockIndex = newTipBlock.Index;
            BlockIndexSubject.OnNext(BlockIndex);
            BlockTipHash = new BlockHash(newTipBlock.Hash.ToByteArray());
            BlockTipHashSubject.OnNext(BlockTipHash);
            _lastTipChangedAt = DateTimeOffset.UtcNow;

            Debug.Log($"[{nameof(RPCAgent)}] Render reorg: {BlockIndex}, {BlockTipHash.ToString()}");
            BlockRenderer.RenderReorg(null, null, null);
        }

        public void OnReorgEnd(byte[] oldTip, byte[] newTip, byte[] branchpoint)
        {
            BlockRenderer.RenderReorgEnd(null, null, null);
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
                    popup.SetCancelCallbackToExit();
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
                var slotAddresses = currentAvatarState.combinationSlotAddresses.ToArray();
                addresses.AddRange(slotAddresses);
            }

            Debug.Log($"Subscribing addresses: {string.Join(", ", addresses)}");

            foreach (var address in addresses)
            {
                Game.Game.instance.CachedAddresses[address] = false;
                if (!Game.Game.instance.CachedStates.ContainsKey(address))
                {
                    Game.Game.instance.CachedStates.Add(address, new Null());
                }
            }

            _service.SetAddressesToSubscribe(Address.ToByteArray(), addresses.Select(addr => addr.ToByteArray()));
        }

        public bool TryGetTxId(Guid actionId, out TxId txId) =>
            _transactions.TryGetValue(actionId, out txId);

        public async UniTask<bool> IsTxStagedAsync(TxId txId) =>
            await _service.IsTransactionStaged(txId.ToByteArray()).ResponseAsync.AsUniTask();
    }
}
