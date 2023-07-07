using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class IAPShopView : MonoBehaviour
    {
        [field:SerializeField]
        public Image ProductImage { get; private set; }

        [field:SerializeField]
        public Button PurchaseButton { get; private set; }

        [field:SerializeField]
        public TextMeshProUGUI PriceText { get; private set; }

        [field:SerializeField]
        public TextMeshProUGUI BuyLimitCountText { get; private set; }

        [field:SerializeField]
        public GameObject LimitCountObject { get; private set; }
    }
}
