using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nekoyume.UI.Module;

namespace Nekoyume.UI
{
    public class ShopListPopup : PopupWidget
    {
        [SerializeField]
        private Image productBgImage;

        [SerializeField]
        private GameObject discountObj;
        [SerializeField]
        private TextMeshProUGUI discountText;

        [SerializeField]
        private IAPRewardView[] iapRewards;

        [SerializeField]
        private GameObject buyLimitObj;
        [SerializeField]
        private TextMeshProUGUI buyLimitText;

        [SerializeField]
        private Button buyButton;

        [SerializeField]
        private Button closeButton;

        protected override void Awake()
        {
            base.Awake();
        
            closeButton.onClick.AddListener(() => { Close(); });
            CloseWidget = () => Close();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

        }
    }
}
