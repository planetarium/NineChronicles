using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Bencodex;
using Bencodex.Types;
using Grpc.Core;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Tx;
using MagicOnion.Client;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.Shared.Hubs;
using Nekoyume.Shared.Services;
using Nekoyume.State;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using static Nekoyume.Action.ActionBase;

namespace Nekoyume.BlockChain
{
    public class RPCAgent : MonoBehaviour, IAgent, IActionEvaluationHubReceiver
    {
        private const float TxProcessInterval = 3.0f;
        private readonly ConcurrentQueue<PolymorphicAction<ActionBase>> _queuedActions =
            new ConcurrentQueue<PolymorphicAction<ActionBase>>();

        private Channel _channel;

        private IActionEvaluationHub _hub;

        private IBlockChainService _service;

        private Codec _codec = new Codec();
        private Subject<ActionEvaluation<ActionBase>> _renderSubject;
        private Subject<ActionEvaluation<ActionBase>> _unrenderSubject;

        private Block<PolymorphicAction<ActionBase>> _genseis;

        public ActionRenderer ActionRenderer { get; private set; }

        public Subject<long> BlockIndexSubject { get; } = new Subject<long>();

        public long BlockIndex { get; private set; }

        public PrivateKey PrivateKey { get; private set; }

        public Address Address => PrivateKey.PublicKey.ToAddress();

        public bool Connected { get; private set; }

        public UnityEvent OnDisconnected { get; private set; }


        public void Initialize(
            CommandLineOptions options,
            PrivateKey privateKey,
            Action<bool> callback)
        {
            Mixpanel.Identify(privateKey.PublicKey.ToAddress().ToString());
            PrivateKey = privateKey;

            _channel = new Channel(
                options.RpcServerHost,
                options.RpcServerPort,
                ChannelCredentials.Insecure
            );
            _hub = StreamingHubClient.Connect<IActionEvaluationHub, IActionEvaluationHubReceiver>(_channel, this);
            _service = MagicOnionClient.Create<IBlockChainService>(_channel);

            RegisterDisconnectEvent(_hub);

            StartCoroutine(CoTxProcessor());
            StartCoroutine(CoJoin(callback));

            OnDisconnected = new UnityEvent();

            _genseis = BlockHelper.ImportBlock(options.GenesisBlockPath ?? BlockHelper.GenesisBlockPath);
        }

        public IValue GetState(Address address)
        {
            byte[] raw = _service.GetState(address.ToByteArray()).ResponseAsync.Result;
            return _codec.Decode(raw);
        }

        public BigInteger GetBalance(Address address, Currency currency)
        {
            var result = _service.GetBalance(
                address.ToByteArray(),
                _codec.Encode(currency.Serialize())
            );
            byte[] raw = result.ResponseAsync.Result;
            return (Bencodex.Types.Integer) _codec.Decode(raw);
        }

        public BigInteger GetBalance(Address address) =>
            GetBalance(address, Currencies.Gold);

        public void EnqueueAction(GameAction action)
        {
            _queuedActions.Enqueue(action);
        }

        #region Mono

        private void Awake()
        {
            _renderSubject = new Subject<ActionEvaluation<ActionBase>>();
            _unrenderSubject = new Subject<ActionEvaluation<ActionBase>>();
            ActionRenderer = new ActionRenderer(_renderSubject, _unrenderSubject);
        }

        private async void OnDestroy()
        {
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
                await _hub.JoinAsync();
            });

            yield return new WaitUntil(() => t.IsCompleted);

            if (t.IsFaulted)
            {
                callback(false);
                yield break;
            }

            Connected = true;

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

            if (GetState(GameConfigState.Address) is Dictionary configDict)
            {
                States.Instance.SetGameConfigState(new GameConfigState(configDict));
            }
            else
            {
                throw new FailedToInstantiateStateException<GameConfigState>();
            }

            // 에이전트의 상태를 한 번 동기화 한다.
            States.Instance.SetAgentState(
                GetState(Address) is Bencodex.Types.Dictionary agentDict
                    ? new AgentState(agentDict)
                    : new AgentState(Address),
                new GoldBalanceState(Address, GetBalance(Address))
            );

            // 그리고 모든 액션에 대한 랜더와 언랜더를 핸들링하기 시작한다.
            ActionRenderHandler.Instance.Start(ActionRenderer);
            ActionUnrenderHandler.Instance.Start(ActionRenderer);

            callback(true);
        }

        private IEnumerator CoTxProcessor()
        {
            while (true)
            {
                yield return new WaitForSeconds(TxProcessInterval);

                var actions = new List<PolymorphicAction<ActionBase>>();
                while (_queuedActions.TryDequeue(out PolymorphicAction<ActionBase> action))
                {
                    actions.Add(action);
                }

                if (actions.Any())
                {
                    Task task = Task.Run(async () =>
                    {
                        await MakeTransaction(actions);
                    });
                    yield return new WaitUntil(() => task.IsCompleted);

                    if (task.IsFaulted)
                    {
                        Debug.LogException(task.Exception);
                        Debug.LogError(
                            "Unexpected exception occurred. re-enqueue actions for retransmission."
                        );

                        foreach (var action in actions)
                        {
                            _queuedActions.Enqueue(action);
                        }
                    }
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
                    _genseis?.Hash,
                    actions
                );
            await _service.PutTransaction(tx.Serialize(true));
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
                _renderSubject.OnNext(ev);
            }
        }

        public void OnTipChanged(long index)
        {
            BlockIndex = index;
            BlockIndexSubject.OnNext(index);
        }

        private async void RegisterDisconnectEvent(IActionEvaluationHub hub)
        {
            try
            {
                await hub.WaitForDisconnect();
            }
            finally
            {
                OnDisconnected?.Invoke();
            }
        }
    }
}
