using System.Linq;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    public class MobileShop : Widget
    {
        [SerializeField]
        private IAPShopView view;

        [SerializeField]
        private InAppProductTab originProductTab;

        [SerializeField]
        private Transform productTabParent;

        private bool _productInitialized;

        protected override void Awake()
        {
            base.Awake();
            view.PurchaseButton.onClick.AddListener(() =>
            {
                var storeManager = Game.Game.instance.IAPStoreManager;
                storeManager.OnPurchaseClicked(storeManager.Products.First().GoogleSku);
            });
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (!_productInitialized)
            {
                var storeManager = Game.Game.instance.IAPStoreManager;
                foreach (var product in storeManager.Products)
                {
                    var tab = Instantiate(originProductTab, productTabParent);
                    tab.SetText(product.GoogleSku);
                }

                _productInitialized = true;
            }

            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Game.Event.OnRoomEnter.Invoke(true);
            base.Close(ignoreCloseAnimation);
        }
    }
}
