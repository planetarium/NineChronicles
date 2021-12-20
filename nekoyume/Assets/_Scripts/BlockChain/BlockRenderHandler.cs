using System;
using System.Collections.Generic;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c.Renderer;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.Blocks;
using Nekoyume.Action;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using UnityEngine;
using static Lib9c.SerializeKeys;

namespace Nekoyume.BlockChain
{
    using Nekoyume.UI.Scroller;
    using UniRx;
    using NCAction = PolymorphicAction<ActionBase>;
    using NCBlock = Block<PolymorphicAction<ActionBase>>;

    public class BlockRenderHandler
    {
        private static class Singleton
        {
            internal static readonly BlockRenderHandler Value = new BlockRenderHandler();
        }

        public static BlockRenderHandler Instance => Singleton.Value;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private BlockRenderer _blockRenderer;

        private BlockRenderHandler()
        {
        }

        public void Start(BlockRenderer blockRenderer)
        {
            _blockRenderer = blockRenderer ?? throw new ArgumentNullException(nameof(blockRenderer));

            Stop();
            _blockRenderer.BlockSubject
                .ObserveOnMainThread()
                .Subscribe(tuple =>
                {
                    Debug.Log($"[{nameof(BlockRenderHandler)}] Render beginning: {tuple.NewTip?.Index ?? default}, {tuple.NewTip?.ToString() ?? "null"}");
                    UpdateWhenEveryBlockRenderBeginning();
                }).AddTo(_disposables);
            _blockRenderer.ReorgSubject
                .ObserveOnMainThread()
                .Subscribe(tuple =>
                {
                    Debug.Log($"[{nameof(BlockRenderHandler)}] Reorg beginning: {tuple.NewTip?.Index ?? default}, {tuple.NewTip?.ToString() ?? "null"}");
                    var msg = L10nManager.Localize("ERROR_REORG_OCCURRED");
                    UI.NotificationSystem.Push(Model.Mail.MailType.System, msg, NotificationCell.NotificationType.Alert);
                })
                .AddTo(_disposables);
            _blockRenderer.ReorgEndSubject.ObserveOnMainThread().Subscribe(_ =>
            {
                Debug.Log($"[{nameof(BlockRenderHandler)}] Reorg end");
            }).AddTo(_disposables);
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }

        private static void UpdateWhenEveryBlockRenderBeginning()
        {
            if (States.Instance.AgentState != null)
            {
                var task = UniTask.Run(async () =>
                {
                    FungibleAssetValue value;
                    try
                    {
                        value = await Game.Game.instance.Agent.GetBalanceAsync(
                            States.Instance.AgentState.address,
                            States.Instance.GoldBalanceState.Gold.Currency);
                    }
                    catch (Exception e)
                    {
                        return e;
                    }

                    AgentStateSubject.OnNextGold(value);
                    return null;
                });
                task.GetAwaiter().OnCompleted(() =>
                {
                    var exception = task.GetAwaiter().GetResult();
                    if (exception is OperationCanceledException)
                    {
                        return;
                    }

                    Debug.LogException(exception);
                });
            }

            if (States.Instance.CurrentAvatarState != null)
            {
                var task = UniTask.Run(async () =>
                {
                    IValue value;
                    try
                    {
                        value = await Game.Game.instance.Agent.GetStateAsync(States.Instance.CurrentAvatarState
                            .address);
                    }
                    catch (Exception e)
                    {
                        return e;
                    }

                    if (!(value is Bencodex.Types.Dictionary dict))
                    {
                        return new InvalidCastException(
                            $"value cannot cast to {typeof(Bencodex.Types.Dictionary).FullName}");
                    }

                    var ap = dict.ContainsKey(ActionPointKey)
                        ? (int)(Bencodex.Types.Integer)dict[ActionPointKey]
                        : dict.ContainsKey(LegacyActionPointKey)
                            ? (int)(Bencodex.Types.Integer)dict[LegacyActionPointKey]
                            : 0;
                    ReactiveAvatarState.UpdateActionPoint(ap);

                    var bi = dict.ContainsKey(DailyRewardReceivedIndexKey)
                        ? (int)(Bencodex.Types.Integer)dict[DailyRewardReceivedIndexKey]
                        : dict.ContainsKey(LegacyDailyRewardReceivedIndexKey)
                            ? (int)(Bencodex.Types.Integer)dict[LegacyDailyRewardReceivedIndexKey]
                            : 0;
                    ReactiveAvatarState.UpdateDailyRewardReceivedIndex(bi);
                    return null;
                });
                task.GetAwaiter().OnCompleted(() =>
                {
                    var exception = task.GetAwaiter().GetResult();
                    if (exception is OperationCanceledException)
                    {
                        return;
                    }

                    Debug.LogException(exception);
                });
            }

            UpdateWeeklyArenaState();
            
            // NOTE: Unregister actions created before 300 blocks for optimization.
            // 300 * 12s = 3600s = 1h
            LocalLayerActions.Instance.UnregisterCreatedBefore(Game.Game.instance.Agent.BlockIndex - 1000);
        }

        private static void UpdateWeeklyArenaState()
        {
            var doNothing = true;
            var agent = Game.Game.instance.Agent;
            var gameConfigState = States.Instance.GameConfigState;
            var challengeCountResetBlockIndex = States.Instance.WeeklyArenaState.ResetIndex;
            var currentBlockIndex = agent.BlockIndex;
            if (currentBlockIndex % gameConfigState.WeeklyArenaInterval == 0 &&
                currentBlockIndex >= gameConfigState.WeeklyArenaInterval)
            {
                doNothing = false;
            }

            if (currentBlockIndex - challengeCountResetBlockIndex >=
                gameConfigState.DailyArenaInterval)
            {
                doNothing = false;
            }

            if (doNothing)
            {
                return;
            }

            var weeklyArenaIndex =
                (int) currentBlockIndex / gameConfigState.WeeklyArenaInterval;
            var weeklyArenaAddress = WeeklyArenaState.DeriveAddress(weeklyArenaIndex);

            var task = UniTask.Run(async () =>
            {
                WeeklyArenaState weeklyArenaState;
                try
                {
                    weeklyArenaState = new WeeklyArenaState(
                        (Bencodex.Types.Dictionary)await agent.GetStateAsync(weeklyArenaAddress));
                }
                catch (Exception e) when (!(e is OperationCanceledException))
                {
                    return e;
                }

                States.Instance.SetWeeklyArenaState(weeklyArenaState);
                return null;
            });
            task.GetAwaiter().OnCompleted(() =>
            {
                var exception = task.GetAwaiter().GetResult();
                if (exception is OperationCanceledException)
                {
                    return;
                }

                Debug.LogException(exception);
            });
        }
    }
}
