using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Module;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using UnityEngine;

namespace Nekoyume.UI
{
    public class MobileShop : Widget
    {
        [SerializeField]
        private Toggle originCategoryTab;

        [SerializeField]
        private IAPShopProductCellView originProductCellView;

        [SerializeField]
        private UnityEngine.UI.ToggleGroup tabToggleGroup;

        [SerializeField]
        private IAPShopDynamicGridLayoutView iAPShopDynamicGridLayout;

        [SerializeField]
        private GameObject loadDataScreen;

        [SerializeField]
        private GameObject emptyCategoryPannel;

        private bool _isInitializedObj;

        private readonly Dictionary<string, IAPShopProductCellView> _allProductObjs =
            new Dictionary<string, IAPShopProductCellView>();

        private readonly Dictionary<string, List<IAPShopProductCellView>> _allProductObjByCategory =
            new Dictionary<string, List<IAPShopProductCellView>>();

        private readonly Dictionary<string, IAPCategoryTab> _allCategoryTab =
            new Dictionary<string, IAPCategoryTab>();

        private Toggle _recommendedToggle;

        private const string RecommendedString = "Recommended";

        private string _lastSelectedCategory;

        public static L10NSchema MOBILE_L10N_SCHEMA;

        public override void Show(bool ignoreShowAnimation = false)
        {
            ShowAsync(ignoreShowAnimation);
        }

        public async void ShowAsProduct(ProductSchema product, UnityEngine.Purchasing.Product purchasingData)
        {
            await ShowAsync();

            Analyzer.Instance.Track("Unity/Shop/IAP/LobbyPopup/Click", ("product-id", product.Sku));

            var evt = new AirbridgeEvent("IAP_LobbyPopup_Click");
            evt.SetAction(product.Sku);
            evt.AddCustomAttribute("product-id", product.Sku);
            AirbridgeUnity.TrackEvent(evt);

            Find<ShopListPopup>().Show(product, purchasingData).Forget();
        }

        public async void ShowAsTab(string categoryName)
        {
            await ShowAsync();

            if (_allCategoryTab.TryGetValue(categoryName, out var categoryTab))
            {
                var toggle = categoryTab.GetComponent<Toggle>();
                // set to true to trigger OnValueChanged
                toggle.isOn = !toggle.isOn;
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Game.Event.OnRoomEnter.Invoke(true);
            base.Close(ignoreCloseAnimation);
        }

        private async Task ShowAsync(bool ignoreShowAnimation = false)
        {
            var loading = Find<LoadingScreen>();
            loading.Show(LoadingScreen.LoadingType.Shop);

            try
            {
                var categorySchemas = await GetCategorySchemas();
                if (!_isInitializedObj)
                {
                    if (categorySchemas.Count == 0)
                    {
                        loading.Close();
                        base.Show(ignoreShowAnimation);
                        Close();
                        Find<IconAndButtonSystem>().Show(
                            "UI_ERROR",
                            "NOTIFICATION_NO_ENTRY_SHOP",
                            "UI_OK",
                            true,
                            IconAndButtonSystem.SystemType.Information);
                    }

                    await InitializeObj(categorySchemas);

                    _isInitializedObj = true;
                }
                else
                {
                    foreach (var category in categorySchemas)
                    {
                        foreach (var item in category.ProductList)
                        {
                            if (_allProductObjs.TryGetValue(item.Sku, out var cellView))
                            {
                                cellView.SetData(item);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                NcDebug.LogError(e.Message);
                loading.Close();
                base.Show(ignoreShowAnimation);
                Close();
                if (Game.LiveAsset.GameConfig.IsKoreanBuild)
                {
                    Find<IconAndButtonSystem>().Show(
                        "UI_ALERT_NOT_IMPLEMENTED_TITLE",
                        "UI_ALERT_NOT_IMPLEMENTED_CONTENT",
                        "UI_OK",
                        true,
                        IconAndButtonSystem.SystemType.Information);
                }
                else
                {
                    Find<IconAndButtonSystem>().Show(
                        "UI_ERROR",
                        "ERROR_NO_ENTRY_SHOP",
                        "UI_OK",
                        true);
                }
                return;
            }

            base.Show(ignoreShowAnimation);

            if (_recommendedToggle != null)
            {
                _recommendedToggle.isOn = true;
                _recommendedToggle.onObject.SetActive(true);
                _recommendedToggle.offObject.SetActive(true);
                RefreshGridByCategory(RecommendedString);
                _lastSelectedCategory = RecommendedString;
            }

            RefreshAllCategoryNoti();

            AudioController.instance.PlayMusic(AudioController.MusicCode.Shop);
            loading.Close();
        }

        private async Task InitializeObj(IEnumerable<CategorySchema> categorySchemas)
        {
            var renderCategory = categorySchemas
                .Where(c => c.Active && c.Name != "NoShow")
                .OrderBy(c => c.Order);

            foreach (var category in renderCategory)
            {
                if (category == null)
                {
                    NcDebug.LogError("category is null");
                    continue;
                }

                if (_allCategoryTab.ContainsKey(category.Name))
                {
                    NcDebug.LogError($"category {category.Name} is already exist");
                    continue;
                }

                if (_allProductObjByCategory.ContainsKey(category.Name))
                {
                    NcDebug.LogError($"category {category.Name} is already exist");
                    continue;
                }

                var categoryTabObj = Instantiate(originCategoryTab, tabToggleGroup.transform);
                if (categoryTabObj == null)
                {
                    NcDebug.LogError("categoryTabObj is null");
                    continue;
                }

                Sprite iconSprite = null;
                try
                {
                    iconSprite = await Util.DownloadTexture($"{MOBILE_L10N_SCHEMA.Host}/{category.Path}");
                }
                catch (Exception e)
                {
                    NcDebug.LogError(e.Message);
                    NcDebug.LogError($"Failed to download icon: {category.Path}");
                }

                var categoryTab = categoryTabObj.GetComponent<IAPCategoryTab>();

                if (categoryTab == null)
                {
                    NcDebug.LogError("categoryTab is null");
                    continue;
                }

                categoryTab.SetData(category.L10n_Key, iconSprite);

                try
                {
                    categoryTabObj.onObject.SetActive(false);
                    categoryTabObj.offObject.SetActive(true);
                    categoryTabObj.group = tabToggleGroup;
                    tabToggleGroup.RegisterToggle(categoryTabObj);
                    categoryTabObj.onValueChanged.AddListener((isOn) =>
                    {
                        if (!isOn)
                        {
                            return;
                        }

                        AudioController.PlayClick();
                        RefreshGridByCategory(category.Name);
                        _lastSelectedCategory = category.Name;
                    });
                }
                catch (Exception e)
                {
                    NcDebug.LogError(e.Message);
                    NcDebug.LogError("Failed to set category tab");
                    continue;
                }

                _allCategoryTab.Add(category.Name, categoryTab);

                var productList = category.ProductList
                    .Where(p => p.Active)
                    .OrderBy(p => p.Order);

                var iapProductCellObjs = new List<IAPShopProductCellView>();
                foreach (var product in productList)
                {
                    if (!_allProductObjs.TryGetValue(product.Sku, out var productObj))
                    {
                        productObj = Instantiate(originProductCellView, iAPShopDynamicGridLayout.transform);
                        productObj.SetData(product, category.Name == RecommendedString);
                        try
                        {
                            await productObj.RefreshLocalized();
                        }
                        catch (Exception e)
                        {
                            NcDebug.LogError(e.Message);
                            NcDebug.LogError($"Failed to refresh localized: {product.Sku}");
                        }
                        _allProductObjs.Add(product.Sku, productObj);
                    }

                    iapProductCellObjs.Add(productObj);
                }

                _allProductObjByCategory.Add(category.Name, iapProductCellObjs);

                categoryTabObj.interactable = true;

                if (category.Name == RecommendedString)
                {
                    _recommendedToggle = categoryTabObj;
                }
            }
        }

        public static async Task LoadL10Ns()
        {
            MOBILE_L10N_SCHEMA = await Game.Game.instance.IAPServiceManager.L10NAsync();
            await UniTask.SwitchToMainThread();
            await L10nManager.AdditionalL10nTableDownload($"{MOBILE_L10N_SCHEMA.Host}/{MOBILE_L10N_SCHEMA.Category}");
            await L10nManager.AdditionalL10nTableDownload($"{MOBILE_L10N_SCHEMA.Host}/{MOBILE_L10N_SCHEMA.Product}");
        }

        public static async Task<IReadOnlyList<CategorySchema>> GetCategorySchemas()
        {
            return await Game.Game.instance.IAPServiceManager
                .GetProductsAsync(States.Instance.AgentState.address, Game.Game.instance.CurrentPlanetId.ToString());
        }

        public void RefreshGrid()
        {
            RefreshGridByCategory(_lastSelectedCategory);
        }

        public void SetLoadingDataScreen(bool isLoading)
        {
            loadDataScreen.SetActive(isLoading);
        }

        public void PurchaseComplete(string productId)
        {
            if (_allProductObjs.TryGetValue(productId, out var cell))
            {
                cell.LocalPurchaseSuccess();
            }

            RefreshAllCategoryNoti();
        }

        private void RefreshAllCategoryNoti()
        {
            foreach (var item in _allCategoryTab)
            {
                var noti = _allProductObjByCategory[item.Key].Any(product => product.IsNotification());
                item.Value.SetNoti(noti);
            }
        }

        private void RefreshGridByCategory(string categoryName)
        {
            Analyzer.Instance.Track("Unity/Shop/IAP/Tab/Click", ("category-name", categoryName));

            var evt = new AirbridgeEvent("IAP_Tab_Click");
            evt.SetAction(categoryName);
            AirbridgeUnity.TrackEvent(evt);

            foreach (var item in _allProductObjs)
            {
                item.Value.gameObject.SetActive(false);
            }

            var buyableItems = _allProductObjByCategory[categoryName].Where(item => item.IsBuyable());
            foreach (var item in buyableItems)
            {
                item.gameObject.SetActive(true);
            }
            emptyCategoryPannel.SetActive(buyableItems.Count() == 0);

            iAPShopDynamicGridLayout.Refresh();
        }

        public static string GetPrice(string isoCurrencyCode, decimal price)
        {
            switch (isoCurrencyCode)
            {
                case "KRW":
                    return $"₩{price:N0}";
                default:
                    return $"{isoCurrencyCode} {price:N2}";
            }
        }

        public static string RemainTimeForDailyLimit
        {
            get
            {
                var now = DateTime.UtcNow;
                var nextDay = now.Date.AddDays(1);
                return (nextDay - now).TimespanToString();
            }
        }

        public static string RemainTimeForWeeklyLimit
        {
            get
            {
                var now = DateTime.UtcNow;

                var daysUntilNextSunday = DayOfWeek.Sunday - now.DayOfWeek;
                if (daysUntilNextSunday <= 0)
                {
                    daysUntilNextSunday += 7;
                }

                var nextSunday = now.Date.AddDays(daysUntilNextSunday);
                return (nextSunday - now).TimespanToString();
            }
        }
    }
}
