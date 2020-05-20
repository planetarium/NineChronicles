using System;
using Assets.SimpleLocalization;
using Libplanet;
using Libplanet.Crypto;
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
            title.text = LocalizationManager.Localize("UI_REDEEM_CODE");
            placeHolder.text = LocalizationManager.Localize("UI_REDEEM_CODE_PLACEHOLDER");
            cancelButtonText.text = LocalizationManager.Localize("UI_CANCEL");
            submitButtonText.text = LocalizationManager.Localize("UI_OK");
        }

        public void RequestRedeemCode()
        {
            var hex = codeField.text;
            PublicKey key;
            try
            {
                key = new PrivateKey(ByteUtil.ParseHex(hex)).PublicKey;
            }
            catch (Exception)
            {
                //TODO 실패 안내
                Close();
                return;
            }
            Game.Game.instance.ActionManager.RedeemCode(key);
            Notification.Push(MailType.System, "Request Redeem Code.");
            Close();
        }
    }
}
