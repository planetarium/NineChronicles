using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

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

        private RectTransform _rect;
        private ProductSchema _data;
        private UnityEngine.Purchasing.Product _puchasingData;

        private void Awake()
        {
            buyButton.onClick.AddListener(()=> {
                if (_data == null || !_data.Buyable)
                    return;

                Widget.Find<ShopListPopup>().Show(_data, _puchasingData);
            });
        }

        public void SetData(ProductSchema data, bool isRecommended)
        {
            _data = data;
            _rect = GetComponent<RectTransform>();

            productName.text = data.Name;


            var isDiscount = false;

            _puchasingData = Game.Game.instance.IAPStoreManager.IAPProducts.First(p => p.definition.id == data.GoogleSku);

            foreach (var item in price)
            {
                item.text = _puchasingData.metadata.localizedPriceString;
            }

            foreach (var item in discountObjs)
            {
                item.SetActive(isDiscount);
            }
            //discount
            if (isDiscount)
            {
                //discount.text = $"{data.DIscount}%";
                foreach (var item in preDiscountPrice)
                {
                    //item.text = 
                }
            }


            buyButton.interactable = data.Buyable;

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
