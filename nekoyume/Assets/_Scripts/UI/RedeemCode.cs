using System;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using TMPro;

namespace Nekoyume.UI
{
    public class RedeemCode : Widget
    {
        public TextMeshProUGUI title;
        public TextMeshProUGUI placeHolder;
        public TextMeshProUGUI cancelButtonText;
        public TextMeshProUGUI submitButtonText;
        public TMP_InputField codeField;

        protected override void Awake()
        {
            base.Awake();
            title.text = L10nManager.Localize("UI_REDEEM_CODE");
            placeHolder.text = L10nManager.Localize("UI_REDEEM_CODE_PLACEHOLDER");
            cancelButtonText.text = L10nManager.Localize("UI_CANCEL");
            submitButtonText.text = L10nManager.Localize("UI_OK");
        }

        public void RequestRedeemCode()
        {
            Game.Game.instance.ActionManager.RedeemCode(codeField.text.Trim());
            Notification.Push(MailType.System, "Request Redeem Code.");
            Close();
        }
    }
}
