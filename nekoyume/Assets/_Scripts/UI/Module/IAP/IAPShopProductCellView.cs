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
        private GameObject disabledBuyButton;

        [SerializeField]
        private Image backgroundImage;
        [SerializeField]
        private Image productImage;

        [SerializeField]
        private VerticalLayoutGroup bottomLayout;
        [SerializeField]
        private LayoutElement bottomButtonLayoutElement;

        private RectTransform _rect;
        private ProductSchema _data;
        private UnityEngine.Purchasing.Product _puchasingData;

        private void Awake()
        {
            buyButton.onClick.AddListener(()=> {
                if (_data == null || !_data.Buyable)
                    return;

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

            buyLimitDescription.gameObject.SetActive(false);
            if (_data.AccountLimit != null)
            {
                buyLimitDescription.gameObject.SetActive(true);
                buyLimitDescription.text = L10nManager.Localize("MOBILE_SHOP_PRODUCT_AccountLimit", _data.AccountLimit.Value) + $" ({_data.AccountLimit.Value - _data.PurchaseCount}/{_data.AccountLimit.Value})"; ;
            }

            if (_data.WeeklyLimit != null)
            {
                buyLimitDescription.gameObject.SetActive(true);
                buyLimitDescription.text = L10nManager.Localize("MOBILE_SHOP_PRODUCT_WeeklyLimit", _data.WeeklyLimit.Value) + $" ({_data.WeeklyLimit.Value - _data.PurchaseCount}/{_data.WeeklyLimit.Value})"; ;
            }
            if (_data.DailyLimit != null)
            {
                buyLimitDescription.gameObject.SetActive(true);
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
                    bottomButtonLayoutElement.minHeight = 65;
                    bottomLayout.spacing = 0;
                    break;
                case "1x2":
                    _rect.sizeDelta = new Vector2(_rect.sizeDelta.x, 467);// add spacing size
                    bottomButtonLayoutElement.minHeight = 75;
                    bottomLayout.spacing = 3;
                    break;
                default:
                    break;
            }

            foreach (var item in price)
            {
                item.text = $"{_puchasingData.metadata.isoCurrencyCode} {_puchasingData.metadata.localizedPrice:N2}";
            }
            Debug.Log($"{_puchasingData.metadata.localizedTitle} : {_puchasingData.metadata.isoCurrencyCode} {_puchasingData.metadata.localizedPriceString} {_puchasingData.metadata.localizedPrice}");

            foreach (var item in discountObjs)
            {
                item.SetActive(isDiscount);
            }
        
            if (isDiscount)
            {
                foreach (var item in preDiscountPrice)
                {
                    var originPrice = (_puchasingData.metadata.localizedPrice * ((decimal)100 / (decimal)(100-_data.Discount)));
                    var origin = $"{_puchasingData.metadata.isoCurrencyCode} {originPrice:N2}";
                    item.text = origin;
                }
                discount.text = $"{_data.Discount}%";
            }
            buyButton.interactable = _data.Buyable;
            disabledBuyButton.SetActive(!buyButton.interactable);
            recommended.SetActive(isRecommended);
        }

        public void LocalPurchaseSucces()
        {
            _data.PurchaseCount++;
        }

        public bool IsBuyable()
        {
            if(_data.AccountLimit != null)
            {
                if(_data.PurchaseCount < _data.AccountLimit.Value)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (_data.DailyLimit != null)
            {
                if (_data.PurchaseCount < _data.DailyLimit.Value)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (_data.WeeklyLimit != null)
            {
                if (_data.PurchaseCount < _data.WeeklyLimit.Value)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
