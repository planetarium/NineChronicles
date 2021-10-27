using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Bencodex;
using Bencodex.Types;
using Grpc.Core;
using Lib9c.Renderer;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Tx;
using MagicOnion.Client;
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
using static Nekoyume.Action.ActionBase;
using Logger = Serilog.Core.Logger;

namespace Nekoyume.BlockChain
{
    using UniRx;

    public class RPCAgent : MonoBehaviour, IAgent, IActionEvaluationHubReceiver
    {
        private const float TxProcessInterval = 1.0f;
        private readonly ConcurrentQueue<PolymorphicAction<ActionBase>> _queuedActions =
            new ConcurrentQueue<PolymorphicAction<ActionBase>>();

        private readonly TransactionMap _transactions = new TransactionMap(20);

        private Channel _channel;

        private IActionEvaluationHub _hub;

        private IBlockChainService _service;

        private Codec _codec = new Codec();

        private Block<PolymorphicAction<ActionBase>> _genesis;

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

        public int AppProtocolVersion { get; private set; }

        public BlockHash BlockTipHash { get; private set; }

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
            _service = MagicOnionClient.Create<IBlockChainService>(_channel);
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
            yield return null;
        }

        public IValue GetState(Address address)
        {
            byte[] raw = _service.GetState(address.ToByteArray()).ResponseAsync.Result;
            return _codec.Decode(raw);
        }

        public async Task<IValue> GetStateAsync(Address address)
        {
            byte[] raw = await _service.GetState(address.ToByteArray());
            return _codec.Decode(raw);
        }

        public FungibleAssetValue GetBalance(Address address, Currency currency)
        {
            // FIXME: `CurrencyExtension.Serialize()` should be changed to `Currency.Serialize()`.
            var result = _service.GetBalance(
                address.ToByteArray(),
                _codec.Encode(CurrencyExtensions.Serialize(currency))
            );
            byte[] raw = result.ResponseAsync.Result;
            var serialized = (Bencodex.Types.List) _codec.Decode(raw);
            return FungibleAssetValue.FromRawValue(
                new Currency(serialized.ElementAt(0)),
                serialized.ElementAt(1).ToBigInteger());
        }

        public async Task<FungibleAssetValue> GetBalanceAsync(Address address, Currency currency)
        {
            // FIXME: `CurrencyExtension.Serialize()` should be changed to `Currency.Serialize()`.
            byte[] raw = await _service.GetBalance(
                address.ToByteArray(),
                _codec.Encode(CurrencyExtensions.Serialize(currency))
            );
            var serialized = (Bencodex.Types.List) _codec.Decode(raw);
            return FungibleAssetValue.FromRawValue(
                new Currency(serialized.ElementAt(0)),
                serialized.ElementAt(1).ToBigInteger());
        }

        public void SendException(Exception exc)
        {
        }

        public void EnqueueAction(GameAction action)
        {
            _queuedActions.Enqueue(action);
        }

        #region Mono

        private async void OnDestroy()
        {
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

                States.Instance.SetAgentState(
                    await GetStateAsync(Address) is Bencodex.Types.Dictionary agentDict
                        ? new AgentState(agentDict)
                        : new AgentState(Address));
                States.Instance.SetGoldBalanceState(
                    new GoldBalanceState(Address, await GetBalanceAsync(Address, goldCurrency)));

                // 랭킹의 상태를 한 번 동기화 한다.
                for (var i = 0; i < RankingState.RankingMapCapacity; ++i)
                {
                    var address = RankingState.Derive(i);
                    var mapState = await GetStateAsync(address) is Bencodex.Types.Dictionary serialized
                        ? new RankingMapState(serialized)
                        : new RankingMapState(address);
                    States.Instance.SetRankingMapStates(mapState);
                }

                // 상점의 상태를 한 번 동기화 한다.

                if (await GetStateAsync(GameConfigState.Address) is Dictionary configDict)
                {
                    States.Instance.SetGameConfigState(new GameConfigState(configDict));
                }
                else
                {
                    throw new FailedToInstantiateStateException<GameConfigState>();
                }

                var weeklyArenaState = await ArenaHelper.GetThisWeekStateAsync(BlockIndex);
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

                if (!_queuedActions.TryDequeue(out PolymorphicAction<ActionBase> action))
                {
                    continue;
                }

                Task task = Task.Run(async () =>
                {
                    await MakeTransaction(new List<PolymorphicAction<ActionBase>> { action });
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

        private async Task MakeTransaction(List<PolymorphicAction<ActionBase>> actions)
        {
            long nonce = await GetNonceAsync();
            Transaction<PolymorphicAction<ActionBase>> tx =
                Transaction<PolymorphicAction<ActionBase>>.Create(
                    nonce,
                    PrivateKey,
                    _genesis?.Hash,
                    actions
                );
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
            var formatter = new BinaryFormatter();
            using (var compressed = new MemoryStream(evaluation))
            using (var decompressed = new MemoryStream())
            using (var df = new DeflateStream(compressed, CompressionMode.Decompress))
            {
                df.CopyTo(decompressed);
                decompressed.Seek(0, SeekOrigin.Begin);
                var ev = (ActionEvaluation<ActionBase>)formatter.Deserialize(decompressed);
                ActionRenderer.ActionRenderSubject.OnNext(ev);
            }
        }

        public void OnUnrender(byte[] evaluation)
        {
            var formatter = new BinaryFormatter();
            using (var compressed = new MemoryStream(evaluation))
            using (var decompressed = new MemoryStream())
            using (var df = new DeflateStream(compressed, CompressionMode.Decompress))
            {
                df.CopyTo(decompressed);
                decompressed.Seek(0, SeekOrigin.Begin);
                var ev = (ActionEvaluation<ActionBase>)formatter.Deserialize(decompressed);
                ActionRenderer.ActionUnrenderSubject.OnNext(ev);
            }
        }

        public void OnRenderBlock(byte[] oldTip, byte[] newTip)
        {
            var dict = (Bencodex.Types.Dictionary)_codec.Decode(newTip);
            HashAlgorithmGetter hashAlgorithmGetter = Game.Game.instance.Agent.BlockPolicySource
                .GetPolicy()
                .GetHashAlgorithm;
            Block<PolymorphicAction<ActionBase>> newTipBlock =
                BlockMarshaler.UnmarshalBlock<PolymorphicAction<ActionBase>>(hashAlgorithmGetter, dict);
            BlockIndex = newTipBlock.Index;
            BlockIndexSubject.OnNext(BlockIndex);
            BlockTipHash = new BlockHash(newTipBlock.Hash.ToByteArray());
            BlockTipHashSubject.OnNext(BlockTipHash);
            _lastTipChangedAt = DateTimeOffset.UtcNow;
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
            var retryCount = 10;
            while (retryCount > 0)
            {
                Debug.Log($"Retry rpc connection. (count: {retryCount})");
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
                    await Join();
                    Debug.Log($"Join complete! Registering disconnect event...");
                    RegisterDisconnectEvent(_hub);
                    UpdateSubscribeAddresses();
                    OnRetryEnded.OnNext(this);
                    return;
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

        private async Task Join()
        {
            await _hub.JoinAsync(Address.ToHex());
            await _service.AddClient(Address.ToByteArray());
        }

        public void OnReorged(byte[] oldTip, byte[] newTip, byte[] branchpoint)
        {
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
                    Widget
                        .Find<TitleOneButtonSystem>()
                        .ShowAndQuit(L10nManager.Localize("UI_ERROR"), errorMsg,
                            L10nManager.Localize("UI_OK"), false);
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
            var addresses = new List<Address> { Address };

            var currentAvatarState = States.Instance.CurrentAvatarState;
            if (!(currentAvatarState is null))
            {
                var slotAddresses = currentAvatarState.combinationSlotAddresses.ToArray();
                addresses.AddRange(slotAddresses);
            }

            Debug.Log($"Subscribing addresses: {string.Join(", ", addresses)}");
            _service.SetAddressesToSubscribe(Address.ToByteArray(), addresses.Select(addr => addr.ToByteArray()));
        }

        public bool IsActionStaged(Guid actionId, out TxId txId)
        {
            return _transactions.TryGetValue(actionId, out txId)
                   && _service.IsTransactionStaged(txId.ToByteArray()).ResponseAsync.Result;
        }
    }
}
