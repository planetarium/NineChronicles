using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UniRx;
using Nekoyume.L10n;
using Cysharp.Threading.Tasks;
using Nekoyume.Helper;

namespace Nekoyume.UI.Module
{
    public class IAPShopProductCellView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI productName;

        [SerializeField]
        private TextMeshProUGUI discount;

        [SerializeField]
        private TextMeshProUGUI[] preDiscountPrice;

        [SerializeField]
        private TextMeshProUGUI[] price;

        [SerializeField]
        private TextMeshProUGUI buyLimitDescription;

        [SerializeField]
        private GameObject[] discountObjs;

        [SerializeField]
        private GameObject recommended;

        [SerializeField]
        private Button buyButton;

        [SerializeField]
        private Image backgroundImage;
        [SerializeField]
        private Image productImage;

        private RectTransform _rect;
        private ProductSchema _data;
        private UnityEngine.Purchasing.Product _puchasingData;

        private void Awake()
        {
            buyButton.onClick.AddListener(()=> {
                /*if (_data == null || !_data.Buyable)
                    return;*/

                Analyzer.Instance.Track("Unity/Shop/IAP/GridCell/Click", ("product-id", _data.GoogleSku));
                Widget.Find<ShopListPopup>().Show(_data, _puchasingData).Forget();
            });

            L10nManager.OnLanguageChange.Subscribe(_ =>
            {
                RefreshLocalized().Forget();
            }).AddTo(gameObject);
        }

        public async UniTask RefreshLocalized()
        {
            productName.text = L10nManager.Localize(_data.L10n_Key);

            buyLimitDescription.transform.parent.gameObject.SetActive(false);
            if (_data.AccountLimit != null)
            {
                buyLimitDescription.transform.parent.gameObject.SetActive(true);
                buyLimitDescription.text = L10nManager.Localize("MOBILE_SHOP_PRODUCT_AccountLimit", _data.AccountLimit.Value) + $" ({_data.AccountLimit.Value - _data.PurchaseCount}/{_data.AccountLimit.Value})"; ;
            }

            if (_data.WeeklyLimit != null)
            {
                buyLimitDescription.transform.parent.gameObject.SetActive(true);
                buyLimitDescription.text = L10nManager.Localize("MOBILE_SHOP_PRODUCT_WeeklyLimit", _data.WeeklyLimit.Value) + $" ({_data.WeeklyLimit.Value - _data.PurchaseCount}/{_data.WeeklyLimit.Value})"; ;
            }
            if (_data.DailyLimit != null)
            {
                buyLimitDescription.transform.parent.gameObject.SetActive(true);
                buyLimitDescription.text = L10nManager.Localize("MOBILE_SHOP_PRODUCT_DailyLimit", _data.DailyLimit.Value) + $" ({_data.DailyLimit.Value - _data.PurchaseCount}/{_data.DailyLimit.Value})"; ;
            }

            await DownLoadImage();
        }

        private async UniTask DownLoadImage()
        {
            var bgImage = await Util.DownloadTexture($"{MobileShop.MOBILE_L10N_SCHEMA.Host}/{_data.BgPath}");
            var iconImage = await Util.DownloadTexture($"{MobileShop.MOBILE_L10N_SCHEMA.Host}/{_data.Path}");
            backgroundImage.sprite = bgImage;
            productImage.sprite = iconImage;
        }

        public void SetData(ProductSchema data, bool isRecommended)
        {
            _data = data;
            _rect = GetComponent<RectTransform>();
            var isDiscount = _data.Discount > 0;
            _puchasingData = Game.Game.instance.IAPStoreManager.IAPProducts.First(p => p.definition.id == data.GoogleSku);

            switch (_data.Size)
            {
                case "1x1":
                    _rect.sizeDelta = new Vector2(_rect.sizeDelta.x, 230);
                    break;
                case "1x2":
                    _rect.sizeDelta = new Vector2(_rect.sizeDelta.x, 467);// add spacing size
                    break;
                default:
                    break;
            }

            foreach (var item in price)
            {
                item.text = _puchasingData.metadata.localizedPriceString;
            }

            foreach (var item in discountObjs)
            {
                item.SetActive(isDiscount);
            }
        
            if (isDiscount)
            {
                foreach (var item in preDiscountPrice)
                {
                    var originPrice = (_puchasingData.metadata.localizedPrice * ((decimal)100 / (decimal)(100-_data.Discount)));
                    var origin = _puchasingData.metadata.localizedPriceString.Replace(_puchasingData.metadata.localizedPrice.ToString(), $"{originPrice:N3}");
                    item.text = origin;
                }
                discount.text = $"{_data.Discount}%";
            }

            /*buyButton.interactable = _data.Buyable;*/


            recommended.SetActive(isRecommended);
            /*            if (isOn)
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
                view.PriceTexts.ForEach(text => text.text = storeProduct.metadata.localizedPriceString);
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
            }*/
        }
    }
}
