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
    }
}
