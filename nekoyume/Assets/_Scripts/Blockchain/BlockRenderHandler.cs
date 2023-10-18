using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c;
using Lib9c.Renderers;
using Libplanet.Types.Assets;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using UnityEngine;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Blockchain
{
    using UI.Scroller;
    using UniRx;

    public class BlockRenderHandler
    {
        private static class Singleton
        {
            internal static readonly BlockRenderHandler Value = new();
        }

        public static BlockRenderHandler Instance => Singleton.Value;

        private readonly List<IDisposable> _disposables = new();

        private BlockRenderer _blockRenderer;
        private static bool _balanceUpdateRequired = true;
        private static bool _avatarUpdateRequired = true;

        private BlockRenderHandler()
        {
        }

        public void Start(BlockRenderer blockRenderer)
        {
            _blockRenderer =
                blockRenderer ?? throw new ArgumentNullException(nameof(blockRenderer));

            Stop();
            _blockRenderer.BlockSubject
                .ObserveOnMainThread()
                .Subscribe(_ => UpdateWhenEveryBlockRenderBeginningAsync().Forget())
                .AddTo(_disposables);
            if (Game.Game.instance.Agent is RPCAgent rpcAgent)
            {
                rpcAgent.OnRetryEnded.Subscribe(_ =>
                {
                    _balanceUpdateRequired = true;
                    _avatarUpdateRequired = true;
                }).AddTo(_disposables);
            }
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }

        private static async UniTaskVoid UpdateWhenEveryBlockRenderBeginningAsync()
        {
            var agent = Game.Game.instance.Agent;
            if (agent is null)
            {
                return;
            }

            var agentState = States.Instance.AgentState;
            if (agentState != null && _balanceUpdateRequired)
            {
                var (hasException, exception) =
                    await UniTask.Run<(bool hasException, Exception exception)>(async () =>
                    {
                        FungibleAssetValue gold;
                        FungibleAssetValue crystal;
                        FungibleAssetValue garage;
                        try
                        {
                            var ncg = States.Instance.NCG;
                            var favArr = await Task.WhenAll(
                                agent.GetBalanceAsync(agentState.address, ncg),
                                agent.GetBalanceAsync(agentState.address, Currencies.Crystal),
                                agent.GetBalanceAsync(agentState.address, Currencies.Garage));
                            gold = favArr[0];
                            crystal = favArr[1];
                            garage = favArr[2];
                        }
                        catch (Exception e)
                        {
                            return (true, e);
                        }

                        AgentStateSubject.OnNextGold(gold);
                        AgentStateSubject.OnNextCrystal(crystal);
                        AgentStateSubject.OnNextGarage(garage);
                        _balanceUpdateRequired = false;
                        return (false, null);
                    });
                if (hasException && exception is not OperationCanceledException)
                {
                    Debug.LogException(exception);
                }
            }

            var currentAvatarState = States.Instance.CurrentAvatarState;
            if (currentAvatarState != null && _avatarUpdateRequired)
            {
                var (hasException, exception) =
                    await UniTask.Run<(bool hasException, Exception exception)>(async () =>
                    {
                        IValue value;
                        try
                        {
                            value = await agent.GetStateAsync(currentAvatarState.address);
                        }
                        catch (Exception e)
                        {
                            return (true, e);
                        }

                        if (value is not Dictionary dict)
                        {
                            return (true, new InvalidCastException(
                                $"value cannot cast to {typeof(Dictionary).FullName}"));
                        }

                        var ap = dict.ContainsKey(ActionPointKey)
                            ? (int)(Integer)dict[ActionPointKey]
                            : dict.ContainsKey(LegacyActionPointKey)
                                ? (Integer)dict[LegacyActionPointKey]
                                : 0;
                        ReactiveAvatarState.UpdateActionPoint(ap);

                        var bi = dict.ContainsKey(DailyRewardReceivedIndexKey)
                            ? (int)(Integer)dict[DailyRewardReceivedIndexKey]
                            : dict.ContainsKey(LegacyDailyRewardReceivedIndexKey)
                                ? (Integer)dict[LegacyDailyRewardReceivedIndexKey]
                                : 0;
                        ReactiveAvatarState.UpdateDailyRewardReceivedIndex(bi);
                        _avatarUpdateRequired = false;
                        return (false, null);
                    });
                if (hasException && !(exception is OperationCanceledException))
                {
                    Debug.LogException(exception);
                }
            }

            // NOTE: Unregister actions created before 300 blocks for optimization.
            // 300 * 12s = 3600s = 1h
            LocalLayerActions.Instance.UnregisterCreatedBefore(agent.BlockIndex - 1000);
        }
    }
}
