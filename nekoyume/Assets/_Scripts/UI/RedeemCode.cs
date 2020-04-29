using Assets.SimpleLocalization;
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
    }
}
