using System;
using Cysharp.Threading.Tasks;
using Nekoyume.Action;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.State.Modifiers;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Scroller;

namespace Nekoyume.BlockChain
{
    public partial class ActionRenderHandlerRenew
    {
        private void OnRenderDailyReward(ActionBase.ActionEvaluation<ActionBase> eval)
        {
            switch (eval.Action)
            {
                case DailyReward dailyReward:
                    OnRenderDailyReward(eval, dailyReward);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(eval));
            }
        }

        private void OnRenderDailyReward(
            ActionBase.ActionEvaluation<ActionBase> eval,
            DailyReward dailyReward)
        {
            if (GameConfigStateSubject.ActionPointState.ContainsKey(dailyReward.avatarAddress))
            {
                GameConfigStateSubject.ActionPointState.Remove(dailyReward.avatarAddress);
            }

            if (eval.Exception is null &&
                dailyReward.avatarAddress == States.Instance.CurrentAvatarState.address)
            {
                LocalLayer.Instance.ClearAvatarModifiers<AvatarDailyRewardReceivedIndexModifier>(
                    dailyReward.avatarAddress);
                UpdateCurrentAvatarStateAsync(eval).Forget();
                UI.NotificationSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_RECEIVED_DAILY_REWARD"),
                    NotificationCell.NotificationType.Notification);
            }
        }
    }
}
