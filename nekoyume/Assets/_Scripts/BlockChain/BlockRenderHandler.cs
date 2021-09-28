using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Lib9c.Renderer;
using Libplanet.Action;
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

        private BlockRenderer _blockRenderer;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private BlockRenderHandler()
        {
        }

        public void Start(BlockRenderer blockRenderer)
        {
            Stop();
            _blockRenderer = blockRenderer;

            Reorg();
            UpdateWeeklyArenaState();

            _blockRenderer.BlockSubject.ObserveOnMainThread().Subscribe(UpdateValues).AddTo(_disposables);
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }

        private void Reorg()
        {
            _blockRenderer.ReorgSubject
                .ObserveOnMainThread()
                .Subscribe(_ =>
                {
                    var msg = L10nManager.Localize("ERROR_REORG_OCCURRED");
                    UI.Notification.Push(Model.Mail.MailType.System, msg);
                })
                .AddTo(_disposables);
        }

        private void UpdateWeeklyArenaState()
        {
            _blockRenderer.EveryBlock()
                .ObserveOnMainThread()
                .Subscribe(_ =>
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
                    var weeklyArenaState =
                        new WeeklyArenaState(
                            (Bencodex.Types.Dictionary) agent.GetState(weeklyArenaAddress));
                    States.Instance.SetWeeklyArenaState(weeklyArenaState);
                })
                .AddTo(_disposables);
        }

        private void UpdateValues((NCBlock OldTip, NCBlock NewTip) tuple)
        {
            if (States.Instance.AgentState != null)
            {
                UniTask.Run(() =>
                {
                    var value = Game.Game.instance.Agent.GetBalance(
                        States.Instance.AgentState.address,
                        States.Instance.GoldBalanceState.Gold.Currency);
                    AgentStateSubject.OnNextGold(value);
                });
            }

            if (States.Instance.CurrentAvatarState != null)
            {
                UniTask.Run(() =>
                {
                    var value = Game.Game.instance.Agent.GetState(States.Instance.CurrentAvatarState.address);
                    if (!(value is Bencodex.Types.Dictionary dict))
                    {
                        return;
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
                });
            }
        }
    }
}
