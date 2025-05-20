using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Nekoyume.ApiClient;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;

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

        [SerializeField]
        private Button closeButton = null;

        private bool _isInitializedObj;

        private readonly Dictionary<string, IAPShopProductCellView> _allProductObjs = new();

        private readonly Dictionary<string, List<IAPShopProductCellView>> _allProductObjByCategory = new();

        private readonly Dictionary<string, IAPCategoryTab> _allCategoryTab = new();

        private Toggle _recommendedToggle;

        private const string RecommendedString = "Recommended";

        private string _lastSelectedCategory;

        public static InAppPurchaseServiceClient.L10NSchema MOBILE_L10N_SCHEMA;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                Lobby.Enter();
                AudioController.PlayClick();
            });

            CloseWidget = () =>
            {
                Close();
                Lobby.Enter();
            };
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            ShowAsync(ignoreShowAnimation);
        }

        public async void ShowAsProduct(InAppPurchaseServiceClient.ProductSchema product, UnityEngine.Purchasing.Product purchasingData)
        {
            await ShowAsync();

            Analyzer.Instance.Track("Unity/Shop/IAP/LobbyPopup/Click", ("product-id", product.Sku()));

            Find<ShopListPopup>().Show(product, purchasingData).Forget();
        }

        public async UniTask ShowAsTab(string categoryName = RecommendedString)
        {
            await ShowAsync();

            SetCategoryTab(categoryName);
        }

        public void SetCategoryTab(string categoryName = RecommendedString)
        {
            if (_allCategoryTab.TryGetValue(categoryName, out var categoryTab))
            {
                var toggle = categoryTab.GetComponent<Toggle>();
                // set to true to trigger OnValueChanged
                toggle.isOn = !toggle.isOn;
            }
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

                    await ApiClients.Instance.IAPServiceManager.RefreshMileageAsync();

                    _isInitializedObj = true;
                }
                else
                {
                    foreach (var category in categorySchemas)
                    {
                        foreach (var item in category.ProductList)
                        {
                            if (_allProductObjs.TryGetValue(item.Sku(), out var cellView))
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
                Lobby.Enter();
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
                // 그리드 Cell의 LayOut이 제대로 갱신 되지 않아서 한 프레임 뒤에 실행
                await UniTask.DelayFrame(1);
                RefreshGridByCategory(RecommendedString);
                _lastSelectedCategory = RecommendedString;
            }

            RefreshAllCategoryNoti();

            AudioController.instance.PlayMusic(AudioController.MusicCode.Shop);
            loading.Close();
        }

        private async Task InitializeObj(IEnumerable<InAppPurchaseServiceClient.CategorySchema> categorySchemas)
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

                categoryTab.SetData(category.L10nKey, iconSprite);

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
                    if (!_allProductObjs.TryGetValue(product.Sku(), out var productObj))
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
                            NcDebug.LogError($"Failed to refresh localized: {product.Sku()}");
                        }

                        _allProductObjs.Add(product.Sku(), productObj);
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
            await UniTask.SwitchToMainThread();
            MOBILE_L10N_SCHEMA = await ApiClients.Instance.IAPServiceManager.L10NAsync();
            await L10nManager.AdditionalL10nTableDownload($"{MOBILE_L10N_SCHEMA.Host}/{MOBILE_L10N_SCHEMA.Category}");
            await L10nManager.AdditionalL10nTableDownload($"{MOBILE_L10N_SCHEMA.Host}/{MOBILE_L10N_SCHEMA.Product}");
        }

        public static async Task<IReadOnlyList<InAppPurchaseServiceClient.CategorySchema>> GetCategorySchemas()
        {
            return await ApiClients.Instance.IAPServiceManager
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
            if(string.IsNullOrEmpty(categoryName))
            {
                return;
            }

            Analyzer.Instance.Track("Unity/Shop/IAP/Tab/Click", ("category-name", categoryName));

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
