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
                Analyzer.Instance.Track(
                    "Unity/Shop/IAP/PurchaseButton/Click",
                    ("product-id", _selectedProductId));
                PurchaseButtonLoadingStart();
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

        public void UpdateView()
        {
            OnToggleValueChanged(true, _productTabDictionary[_selectedProductId]);
        }

        public void PurchaseButtonLoadingStart()
        {
            view.PurchaseButton.interactable = false;
            view.PurchaseButtonDissableGroupObj.SetActive(false);
            view.PurchaseButtonLoadingObj.Show("");
        }

        public void PurchaseButtonLoadingEnd()
        {
            view.PurchaseButton.interactable = true;
            view.PurchaseButtonDissableGroupObj.SetActive(true);
            view.PurchaseButtonLoadingObj.Close();
        }

        private async void ShowAsync(bool ignoreShowAnimation = false)
        {
            var loading = Find<DataLoadingScreen>();
            loading.Show();

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

                    tabToggle.onValueChanged.AddListener(isOn => OnToggleValueChanged(isOn, tab));
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
                tab.Toggle.onObject.SetActive(false);
                tab.Toggle.offObject.SetActive(true);
                tab.Toggle.SetIsOnWithoutNotify(false);
                RefreshToggleValue(false, tab, products);
            }

            if (firstTab != null)
            {
                firstTab.Toggle.onObject.SetActive(true);
                firstTab.Toggle.offObject.SetActive(false);
                firstTab.Toggle.SetIsOnWithoutNotify(true);
                RefreshToggleValue(true, firstTab, products);
            }

            loading.Close();
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

        private async void OnToggleValueChanged(bool isOn, InAppProductTab tab)
        {
            PurchaseButtonLoadingEnd();

            var products = await Game.Game.instance.IAPServiceManager
                .GetProductsAsync(States.Instance.AgentState.address);

            RefreshToggleValue(isOn, tab, products);
        }

        private void RefreshToggleValue(bool isOn, InAppProductTab tab, IReadOnlyList<NineChronicles.ExternalServices.IAPService.Runtime.Models.ProductSchema> products)
        {
            if (isOn)
            {
                Analyzer.Instance.Track(
                    "Unity/Shop/IAP/Tab/Click",
                    ("product-id", tab.ProductId));
                var product = products?.FirstOrDefault(p => p.GoogleSku == tab.ProductId);
                if (product is null)
                {
                    return;
                }

                var storeProduct = Game.Game.instance.IAPStoreManager.IAPProducts.First(p =>
                    p.definition.id == tab.ProductId);

                _selectedProductId = tab.ProductId;
                view.PriceTexts.ForEach(text => text.text = $"{storeProduct.metadata.isoCurrencyCode} {storeProduct.metadata.localizedPrice}");
                view.ProductImage.sprite =
                    _productImageDictionary[GetProductImageNameFromProductId(tab.ProductId)];
                view.PurchaseButton.interactable = product.Buyable;
                var limit = product.DailyLimit ?? product.WeeklyLimit;
                view.LimitCountObjects.ForEach(obj => obj.SetActive(limit.HasValue));
                if (limit.HasValue)
                {
                    var remain = limit - product.PurchaseCount;
                    view.BuyLimitCountText.ForEach(text => text.text = $"{remain}/{limit}");
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

                var messageKey = product.DailyLimit.HasValue
                    ? "UI_MS_BUT_LIMIT_MESSAGE_DAY"
                    : "UI_MS_BUT_LIMIT_MESSAGE_WEEK";
                view.BuyLimitMessageText.text = L10nManager.Localize(messageKey);
            }
        }
    }
}
