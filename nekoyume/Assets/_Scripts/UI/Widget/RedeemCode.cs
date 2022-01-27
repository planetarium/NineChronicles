using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using TMPro;
using UnityEngine.Events;
using Nekoyume.UI.Module;

namespace Nekoyume.UI
{
    using Nekoyume.UI.Scroller;
    using UniRx;

    public class RedeemCode : Widget
    {
        public TMP_InputField codeField;

        public UnityEvent OnRequested = new UnityEvent();

        public override void Show(bool ignoreShowAnimation = false)
        {
            codeField.text = string.Empty;
            base.Show(ignoreShowAnimation);
        }

        public void RequestRedeemCode()
        {
            var code = codeField.text.Trim();
            Close();
            Game.Game.instance.ActionManager.RedeemCode(code).Subscribe();
            NotificationSystem.Push(
                MailType.System,
                L10nManager.Localize("NOTIFICATION_REQUEST_REDEEM_CODE"),
                NotificationCell.NotificationType.Information);
            OnRequested.Invoke();
        }
    }
}
