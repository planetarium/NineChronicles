using System.Linq;
using UnityEngine;
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
                var storeManager = Game.Game.instance.IAPStoreManager;
                storeManager.OnPurchaseClicked(storeManager.Products.First().GoogleSku);
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
