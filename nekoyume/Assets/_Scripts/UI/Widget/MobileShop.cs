using System.Collections.Generic;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.L10n;
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
        private readonly Dictionary<string, Sprite> _productImageDictionary = new();

        protected override void Awake()
        {
            base.Awake();
            foreach (var sprite in Resources.LoadAll<Sprite>("UI/Textures/00_Shop"))
            {
                _productImageDictionary.Add(
                    sprite.name.Remove(0, 4),
                    sprite);
            }
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
                    var storeProduct = Game.Game.instance.IAPStoreManager.IAPProducts.First(p =>
                        p.definition.id == product.GoogleSku);
                    tab.Set(storeProduct, product.DisplayOrder);
                    var tabToggle = tab.Toggle;
                    tabToggle.isOn = false;
                    tabToggle.onObject.SetActive(false);
                    tabToggle.offObject.SetActive(true);
                    tabToggle.group = tabToggleGroup;

                    void OnValueChange(bool isOn)
                    {
                        if (isOn)
                        {
                            _selectedProductId = tab.ProductId;
                            view.PriceText.text = storeProduct.metadata.localizedPriceString;
                            view.ProductImage.sprite =
                                _productImageDictionary[GetProductImageNameFromProductId(product.GoogleSku)];
                            view.PurchaseButton.interactable = product.Buyable;
                            var limit = product.DailyLimit ?? product.WeeklyLimit;
                            view.LimitCountObjects.ForEach(obj => obj.SetActive(limit.HasValue));
                            if (limit.HasValue)
                            {
                                var remain = limit - product.PurchaseCount;
                                view.BuyLimitCountText.text = limit is null
                                    ? string.Empty
                                    : $"{remain}/{limit}";
                            }

                            view.RewardViews.ForEach(v => v.gameObject.SetActive(false));
                            foreach (var fungibleItemSchema in product.FungibleItemList)
                            {
                                var rewardView =
                                    view.RewardViews.First(v => !v.gameObject.activeSelf);
                                rewardView.RewardName.text =
                                    L10nManager.LocalizeItemName(fungibleItemSchema.SheetItemId);
                                rewardView.RewardImage.sprite =
                                    SpriteHelper.GetItemIcon(fungibleItemSchema.SheetItemId);
                                rewardView.RewardCount.text = $"x{fungibleItemSchema.Amount}";
                                rewardView.gameObject.SetActive(true);
                            }
                        }
                    }

                    tabToggle.onValueChanged.AddListener(OnValueChange);
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

        private static string GetProductImageNameFromProductId(string productId)
        {
            return (productId.StartsWith("g_")
                ? productId.Remove(0, 2)
                : productId) + L10nManager.CurrentLanguage switch
            {
                LanguageType.Portuguese => "_PT",
                LanguageType.ChineseSimplified => "_ZH-CN",
                _ => "_EN"
            };
        }
    }
}
