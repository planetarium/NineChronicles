using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UniRx;
using Nekoyume.L10n;
using Cysharp.Threading.Tasks;
using Nekoyume.Helper;
using Nekoyume.State;

namespace Nekoyume.UI.Module
{
    public class IAPShopProductCellView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI productName;

        [SerializeField]
        private GameObject tagObj;

        [SerializeField]
        private TextMeshProUGUI discount;

        [SerializeField]
        private TextMeshProUGUI timeLimitText;

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

        [SerializeField]
        private GameObject dimObj;

        private RectTransform _rect;
        private ProductSchema _data;
        private UnityEngine.Purchasing.Product _purchasingData;

        private void Awake()
        {
            buyButton.onClick.AddListener(() =>
            {
                if (_data == null || !_data.Buyable)
                {
                    return;
                }

                Analyzer.Instance.Track("Unity/Shop/IAP/GridCell/Click", ("product-id", _data.Sku));

                var evt = new AirbridgeEvent("IAP_GridCell_Click");
                evt.SetAction(_data.Sku);
                evt.AddCustomAttribute("product-id", _data.Sku);
                AirbridgeUnity.TrackEvent(evt);

                Widget.Find<ShopListPopup>().Show(_data, _purchasingData).Forget();
            });

            L10nManager.OnLanguageChange
                .Subscribe(_ => RefreshLocalized().Forget())
                .AddTo(gameObject);
        }

        public async UniTask RefreshLocalized()
        {
            productName.text = L10nManager.Localize(_data.L10n_Key);

            buyLimitDescription.gameObject.SetActive(false);
            if (_data.AccountLimit != null)
            {
                buyLimitDescription.gameObject.SetActive(true);
                buyLimitDescription.text =
                    L10nManager.Localize("MOBILE_SHOP_PRODUCT_AccountLimit", _data.AccountLimit.Value) +
                    $" ({_data.AccountLimit.Value - _data.PurchaseCount}/{_data.AccountLimit.Value})";
            }

            if (_data.WeeklyLimit != null)
            {
                buyLimitDescription.gameObject.SetActive(true);
                buyLimitDescription.text =
                    L10nManager.Localize("MOBILE_SHOP_PRODUCT_WeeklyLimit", _data.WeeklyLimit.Value) +
                    $" ({_data.WeeklyLimit.Value - _data.PurchaseCount}/{_data.WeeklyLimit.Value})";
            }

            if (_data.DailyLimit != null)
            {
                buyLimitDescription.gameObject.SetActive(true);
                buyLimitDescription.text =
                    L10nManager.Localize("MOBILE_SHOP_PRODUCT_DailyLimit", _data.DailyLimit.Value) +
                    $" ({_data.DailyLimit.Value - _data.PurchaseCount}/{_data.DailyLimit.Value})";
            }

            await DownLoadImage();
        }

        private async UniTask DownLoadImage()
        {
            backgroundImage.sprite = await Util.DownloadTexture($"{MobileShop.MOBILE_L10N_SCHEMA.Host}/{_data.BgPath}");
            productImage.sprite = await Util.DownloadTexture($"{MobileShop.MOBILE_L10N_SCHEMA.Host}/{_data.Path}");
        }

        public void SetData(ProductSchema data, bool isRecommended)
        {
            _data = data;
            _rect = GetComponent<RectTransform>();
            Refresh();
            recommended.SetActive(isRecommended);
        }

        public void SetData(ProductSchema data)
        {
            _data = data;
            _rect = GetComponent<RectTransform>();
            Refresh();
        }

        private void Refresh()
        {
            _purchasingData = Game.Game.instance.IAPStoreManager.IAPProducts.FirstOrDefault(p => p.definition.id == _data.Sku);
            if (_purchasingData == null && !_data.IsFree)
            {
                gameObject.SetActive(false);
                return;
            }

            if (_data.IsFree)
            {
                _purchasingData = null;
            }

            switch (_data.Size)
            {
                case "1x1":
                    _rect.sizeDelta = new Vector2(_rect.sizeDelta.x, 230);
                    bottomButtonLayoutElement.minHeight = 65;
                    bottomLayout.spacing = 0;
                    break;
                case "1x2":
                    _rect.sizeDelta = new Vector2(_rect.sizeDelta.x, 467); // add spacing size
                    bottomButtonLayoutElement.minHeight = 75;
                    bottomLayout.spacing = 3;
                    break;
            }

            var metadata = _purchasingData?.metadata;
            if (!_data.IsFree)
            {
                NcDebug.Log($"{metadata.localizedTitle} : {metadata.isoCurrencyCode} {metadata.localizedPriceString} {metadata.localizedPrice}");
                foreach (var item in price)
                {
                    item.text = MobileShop.GetPrice(metadata.isoCurrencyCode, metadata.localizedPrice);
                }
            }
            else
            {
                foreach (var item in price)
                {
                    item.text = L10nManager.Localize("MOBILE_SHOP_PRODUCT_IS_FREE");
                }
            }


            var isDiscount = _data.Discount > 0;
            foreach (var item in discountObjs)
            {
                item.SetActive(isDiscount);
            }

            tagObj.SetActive(false);
            discount.gameObject.SetActive(false);
            timeLimitText.gameObject.SetActive(false);
            if (isDiscount && !_data.IsFree)
            {
                discount.text = $"{_data.Discount}%";
                foreach (var item in preDiscountPrice)
                {
                    var originPrice = metadata.localizedPrice * ((decimal)100 / (100 - _data.Discount));
                    var origin = MobileShop.GetPrice(metadata.isoCurrencyCode, originPrice);
                    item.text = origin;
                }
                discount.gameObject.SetActive(true);
                tagObj.SetActive(true);
            }
            else if (_data.DailyLimit != null)
            {
                timeLimitText.text = MobileShop.RemainTimeForDailyLimit;
                timeLimitText.gameObject.SetActive(true);
                tagObj.SetActive(true);
            }
            else if (_data.WeeklyLimit != null)
            {
                timeLimitText.text = MobileShop.RemainTimeForWeeklyLimit;
                timeLimitText.gameObject.SetActive(true);
                tagObj.SetActive(true);
            }

            buyButton.interactable = _data.Buyable;
            RefreshDim();
        }

        private void RefreshDim()
        {
            if (!_data.Buyable)
            {
                dimObj.SetActive(true);
                disabledBuyButton.SetActive(true);
                return;
            }

            var dim = false;
            if (_data.DailyLimit != null)
            {
                dim = _data.PurchaseCount >= _data.DailyLimit.Value;
            }
            else if (_data.WeeklyLimit != null)
            {
                dim = _data.PurchaseCount >= _data.WeeklyLimit.Value;
            }

            dimObj.SetActive(dim);
            disabledBuyButton.SetActive(dim);
        }

        public void LocalPurchaseSuccess()
        {
            _data.PurchaseCount++;
            Refresh();
            RefreshLocalized().Forget();
        }

        public bool IsBuyable()
        {
            if (_data.AccountLimit != null)
            {
                return _data.PurchaseCount < _data.AccountLimit.Value;
            }

            return true;
        }

        public bool IsNotification()
        {
            if (!_data.IsFree)
            {
                return false;
            }

            if (!IsBuyable())
            {
                return false;
            }

            if (dimObj.activeSelf)
            {
                return false;
            }

            if (_data.RequiredLevel == null)
            {
                return true;
            }

            if (_data.RequiredLevel.Value < States.Instance.CurrentAvatarState.level)
            {
                return true;
            }

            return false;
        }

        public int GetOrder()
        {
            if(_data == null)
            {
                return 0;
            }

            return _data.Order;
        }
    }
}
