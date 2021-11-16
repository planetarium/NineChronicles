using System;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using TMPro;
using UnityEngine.Events;

namespace Nekoyume.UI
{
    public class RedeemCode : Widget
    {
        public TextMeshProUGUI title;
        public TextMeshProUGUI placeHolder;
        public TextMeshProUGUI cancelButtonText;
        public TextMeshProUGUI submitButtonText;
        public TMP_InputField codeField;

        public UnityEvent OnRequested = new UnityEvent();

        protected override void Awake()
        {
            base.Awake();
            title.text = L10nManager.Localize("UI_REDEEM_CODE");
            placeHolder.text = L10nManager.Localize("UI_REDEEM_CODE_PLACEHOLDER");
            cancelButtonText.text = L10nManager.Localize("UI_CANCEL");
            submitButtonText.text = L10nManager.Localize("UI_OK");
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
            Find<CodeRewardPopup>().AddSealedCode(code);
            Game.Game.instance.ActionManager.RedeemCode(code).Subscribe();
            NotificationSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_REQUEST_REDEEM_CODE"));
            OnRequested.Invoke();
        }
    }
}
