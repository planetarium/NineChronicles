using Assets.SimpleLocalization;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using TMPro;

namespace Nekoyume.UI
{
    public class RedeemCode : Widget
    {
        public TextMeshProUGUI title;
        public TextMeshProUGUI placeHolder;
        public TextMeshProUGUI cancelButtonText;
        public TextMeshProUGUI submitButtonText;

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
            Game.Game.instance.ActionManager.RedeemCode(PromotionCodeState.Address);
            Notification.Push(MailType.System, "Request Redeem Code.");
            Close();
        }
    }
}
