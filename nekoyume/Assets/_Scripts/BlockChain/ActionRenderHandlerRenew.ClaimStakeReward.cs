using Cysharp.Threading.Tasks;
using Nekoyume.Action;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.UI;
using Nekoyume.UI.Scroller;

namespace Nekoyume.BlockChain
{
    public partial class ActionRenderHandlerRenew
    {
        private void OnRenderClaimStakeReward(ActionBase.ActionEvaluation<ActionBase> eval)
        {
            // NOTE: ClaimStakeReward object(i.e., `eval.Action`) is useless currently.
            //       So it is not needed to be passed to `OnRenderClaimStakeReward()`.
            if (eval.Exception is not null)
            {
                return;
            }

            // Notification
            NotificationSystem.Push(
                MailType.System,
                L10nManager.Localize("NOTIFICATION_CLAIM_MONSTER_COLLECTION_REWARD_COMPLETE"),
                NotificationCell.NotificationType.Information);

            UpdateCurrentAvatarStateAsync(eval).Forget();
        }
    }
}
