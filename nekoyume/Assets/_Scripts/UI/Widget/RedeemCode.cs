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
        public TextMeshProUGUI title;
        public TextMeshProUGUI placeHolder;
        public TextButton cancelButton;
        public TextButton submitButton;
        public TMP_InputField codeField;

        public UnityEvent OnRequested = new UnityEvent();

        protected override void Awake()
        {
            base.Awake();
            title.text = L10nManager.Localize("UI_REDEEM_CODE");
            placeHolder.text = L10nManager.Localize("UI_REDEEM_CODE_PLACEHOLDER");
            cancelButton.Text = L10nManager.Localize("UI_CANCEL");
            submitButton.Text = L10nManager.Localize("UI_OK");
        }

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
