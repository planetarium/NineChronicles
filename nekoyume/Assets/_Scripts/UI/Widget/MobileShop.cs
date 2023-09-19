using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.Game.Controller;
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
        private Toggle originCategoryTab;

        [SerializeField]
        private IAPShopProductCellView originProductCellView;

        [SerializeField]
        private UnityEngine.UI.ToggleGroup tabToggleGroup;

        [SerializeField]
        private IAPShopDynamicGridLayoutView iAPShopDynamicGridLayout;

        private bool _isInitailizedObj;
        private Dictionary<string, IAPShopProductCellView> _allProductObjs = new Dictionary<string, IAPShopProductCellView>();
        private Dictionary<string, List<IAPShopProductCellView>> _allProductObjByCategory = new Dictionary<string, List<IAPShopProductCellView>>();

        private Toggle _recommendedToggle;

        private const string _recommendedString = "Recommended";

        protected override void Awake()
        {
            base.Awake();
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
            var loading = Find<DataLoadingScreen>();
            loading.Show();

            await L10nManager.AdditionalL10nTableDownload("https://dhyrkl3xgx6tk.cloudfront.net/shop/l10n/category.csv");
            await L10nManager.AdditionalL10nTableDownload("https://dhyrkl3xgx6tk.cloudfront.net/shop/l10n/product.csv");

            var categorySchemas = await Game.Game.instance.IAPServiceManager
                .GetProductsAsync(States.Instance.AgentState.address);

            try
            {
                if (!_isInitailizedObj)
                {
                    var renderCategory = categorySchemas.Where(c => c.Active).OrderBy(c => c.Order);
                    foreach (var category in renderCategory)
                    {
                        var categoryTabObj = Instantiate(originCategoryTab, tabToggleGroup.transform);
                        categoryTabObj.GetComponent<IAPCategoryTab>().SetData(category.L10n_Key);

                        categoryTabObj.onObject.SetActive(false);
                        categoryTabObj.offObject.SetActive(true);
                        categoryTabObj.group = tabToggleGroup;
                        tabToggleGroup.RegisterToggle(categoryTabObj);
                        categoryTabObj.onValueChanged.AddListener((isOn) =>
                        {
                            if (!isOn)
                                return;

                            AudioController.PlayClick();
                            RefreshGridByCategory(category.Name);
                        });

                        var productList = category.ProductList?.Where(p => p.Active).OrderBy(p => p.Order);
                        var iapProductCellObjs = new List<IAPShopProductCellView>();
                        foreach (var product in productList)
                        {
                            if (!_allProductObjs.TryGetValue(product.GoogleSku, out var productObj))
                            {
                                productObj = Instantiate(originProductCellView, iAPShopDynamicGridLayout.transform);
                                productObj.SetData(product, category.Name == _recommendedString);
                                _allProductObjs.Add(product.GoogleSku, productObj);
                            }
                            iapProductCellObjs.Add(productObj);
                        }
                        _allProductObjByCategory.Add(category.Name, iapProductCellObjs);

                        categoryTabObj.interactable = true;

                        if (category.Name == _recommendedString)
                            _recommendedToggle = categoryTabObj;
                    }
                }
            }
            catch
            {
                loading.Close();
            }

            base.Show(ignoreShowAnimation);

            _recommendedToggle.isOn = true;
            _recommendedToggle.onObject.SetActive(true);
            _recommendedToggle.offObject.SetActive(true);
            RefreshGridByCategory(_recommendedString);


            _isInitailizedObj = true;
            loading.Close();
        }

        private void RefreshGridByCategory(string categoryName)
        {
            Analyzer.Instance.Track("Unity/Shop/IAP/Tab/Click",("category-name", categoryName));
            foreach (var item in _allProductObjs)
            {
                item.Value.gameObject.SetActive(false);
            }
            foreach (var item in _allProductObjByCategory[categoryName])
            {
                item.gameObject.SetActive(true);
            }
            iAPShopDynamicGridLayout.Refresh();
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
