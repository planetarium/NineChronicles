using System;
using System.Collections.Generic;
using Lib9c.Renderer;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using UniRx;

namespace Nekoyume.BlockChain
{
    using UniRx;

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
    }
}
