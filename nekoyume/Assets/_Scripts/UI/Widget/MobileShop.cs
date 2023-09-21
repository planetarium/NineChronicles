using System;
using System.Collections.Generic;
using System.Linq;
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

        private bool _isInitailizedObj;
        private Dictionary<string, IAPShopProductCellView> _allProductObjs = new Dictionary<string, IAPShopProductCellView>();
        private Dictionary<string, List<IAPShopProductCellView>> _allProductObjByCategory = new Dictionary<string, List<IAPShopProductCellView>>();

        private Toggle _recommendedToggle;

        private const string _recommendedString = "Recommended";

        private string _lastSelectedCategory;

        public static L10NSchema MOBILE_L10N_SCHEMA;

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

            try
            {
                if (!_isInitailizedObj)
                {
                    MOBILE_L10N_SCHEMA = await Game.Game.instance.IAPServiceManager.L10NAsync();

                    await L10nManager.AdditionalL10nTableDownload($"{MOBILE_L10N_SCHEMA.Host}/{MOBILE_L10N_SCHEMA.Category}");
                    await L10nManager.AdditionalL10nTableDownload($"{MOBILE_L10N_SCHEMA.Host}/{MOBILE_L10N_SCHEMA.Product}");

                    var categorySchemas = await Game.Game.instance.IAPServiceManager
                        .GetProductsAsync(States.Instance.AgentState.address);

                    if(categorySchemas.Count == 0)
                    {
                        loading.Close();
                        base.Show(ignoreShowAnimation);
                        Close();
                        Widget.Find<IconAndButtonSystem>().Show(
                            "UI_ERROR",
                            "NOTIFICATION_NO_ENTRY_SHOP",
                            "UI_OK",
                            true,
                            IconAndButtonSystem.SystemType.Information);
                    }

                    var renderCategory = categorySchemas.Where(c => c.Active).OrderBy(c => c.Order);
                    foreach (var category in renderCategory)
                    {
                        var categoryTabObj = Instantiate(originCategoryTab, tabToggleGroup.transform);

                        var iconSprite = await Util.DownloadTexture($"{MOBILE_L10N_SCHEMA.Host}/{category.Path}");
                        categoryTabObj.GetComponent<IAPCategoryTab>().SetData(category.L10n_Key, iconSprite);

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
                            _lastSelectedCategory = category.Name;
                        });

                        var productList = category.ProductList?.Where(p => p.Active).OrderBy(p => p.Order);
                        var iapProductCellObjs = new List<IAPShopProductCellView>();
                        foreach (var product in productList)
                        {
                            if (!_allProductObjs.TryGetValue(product.GoogleSku, out var productObj))
                            {
                                productObj = Instantiate(originProductCellView, iAPShopDynamicGridLayout.transform);
                                productObj.SetData(product, category.Name == _recommendedString);
                                await productObj.RefreshLocalized();
                                _allProductObjs.Add(product.GoogleSku, productObj);
                            }
                            iapProductCellObjs.Add(productObj);
                        }
                        _allProductObjByCategory.Add(category.Name, iapProductCellObjs);

                        categoryTabObj.interactable = true;

                        if (category.Name == _recommendedString)
                            _recommendedToggle = categoryTabObj;
                    }
                    _isInitailizedObj = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                loading.Close();
                base.Show(ignoreShowAnimation);
                Close();
                Widget.Find<IconAndButtonSystem>().Show(
                    "UI_ERROR",
                    "ERROR_NO_ENTRY_SHOP",
                    "UI_OK",
                    true);
                return;
            }

            base.Show(ignoreShowAnimation);

            if(_recommendedToggle != null)
            {
                _recommendedToggle.isOn = true;
                _recommendedToggle.onObject.SetActive(true);
                _recommendedToggle.offObject.SetActive(true);
                RefreshGridByCategory(_recommendedString);
                _lastSelectedCategory = _recommendedString;
            }

            loading.Close();
        }

        public void RefreshGrid()
        {
            RefreshGridByCategory(_lastSelectedCategory);
        }

        public void PurchaseComplete(string productId)
        {
            if(_allProductObjs.TryGetValue(productId, out var cell))
            {
                cell.LocalPurchaseSucces();
            }
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
                if(item.IsBuyable())
                    item.gameObject.SetActive(true);
            }
            iAPShopDynamicGridLayout.Refresh();
        }
    }
}
