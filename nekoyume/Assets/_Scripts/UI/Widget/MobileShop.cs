using System.Collections.Generic;
using System.Linq;
using Nekoyume.State;
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
        private UnityEngine.UI.ToggleGroup tabToggleGroup;

        private readonly Dictionary<string, InAppProductTab> _productTabDictionary = new();
        private bool _productInitialized;
        private string _selectedProductId;

        protected override void Awake()
        {
            base.Awake();
            view.PurchaseButton.onClick.AddListener(() =>
            {
                Debug.LogError($"Purchase: {_selectedProductId}");
                Game.Game.instance.IAPStoreManager.OnPurchaseClicked(_selectedProductId);
            });
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            ShowAsync(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Game.Event.OnRoomEnter.Invoke(true);
            base.Close(ignoreCloseAnimation);
        }

        private async void ShowAsync(bool ignoreShowAnimation = false)
        {
            var products = await Game.Game.instance.IAPServiceManager
                .GetProductsAsync(States.Instance.AgentState.address);
            if (!_productInitialized && products != null)
            {
                foreach (var product in products.OrderBy(p => p.DisplayOrder))
                {
                    var tab = Instantiate(originProductTab, tabToggleGroup.transform);
                    tab.Set(Game.Game.instance.IAPStoreManager.IAPProducts.First(p =>
                            p.definition.id == product.GoogleSku),
                        product.DisplayOrder);
                    var tabToggle = tab.Toggle;
                    tabToggle.isOn = false;
                    tabToggle.onObject.SetActive(false);
                    tabToggle.offObject.SetActive(true);
                    tabToggle.group = tabToggleGroup;
                    tabToggle.onValueChanged.AddListener(isOn =>
                    {
                        if (isOn)
                        {
                            _selectedProductId = tab.ProductId;
                        }
                    });
                    _productTabDictionary.Add(product.GoogleSku, tab);
                }

                _productInitialized = true;
            }


            if (products != null)
            {
                foreach (var product in products.Where(p => !p.Active))
                {
                    if (_productTabDictionary.TryGetValue(product.GoogleSku, out var tab))
                    {
                        tab.gameObject.SetActive(false);
                    }
                }
            }

            base.Show(ignoreShowAnimation);
            InAppProductTab firstTab = null;
            foreach (var tab in _productTabDictionary.Values.OrderBy(tab => tab.DisplayOrder))
            {
                if (products?.Any(p => p.GoogleSku == tab.ProductId && !p.Active) ?? false)
                {
                    tab.gameObject.SetActive(false);
                    continue;
                }

                firstTab ??= tab;
                tab.Toggle.isOn = false;
            }

            if (firstTab != null)
            {
                firstTab.Toggle.isOn = true;
            }
        }
    }
}
