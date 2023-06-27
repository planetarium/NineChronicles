using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class MobileShop : Widget
    {
        [SerializeField]
        private Button purchaseButton;

        protected override void Awake()
        {
            base.Awake();
            purchaseButton.onClick.AddListener(() =>
            {
                Game.Game.instance.IAPStoreManager.OnPurchaseClicked("g_single_ap01");
            });
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Game.Event.OnRoomEnter.Invoke(true);
            base.Close(ignoreCloseAnimation);
        }
    }
}
