using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UniRx;
using Nekoyume.L10n;
using Cysharp.Threading.Tasks;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.ApiClient;

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

        [SerializeField]
        private IAPRewardView[] rewardViews;
        [SerializeField]
        private CenteredGridLayout rewardLayout;
        [SerializeField]
        private GameObject mileageObj;
        [SerializeField]
        private TextMeshProUGUI mileageText;

        private RectTransform _rect;
        private InAppPurchaseServiceClient.ProductSchema _data;
        private UnityEngine.Purchasing.Product _purchasingData;

        private void Awake()
        {
            buyButton.onClick.AddListener(() =>
            {
                if (_data == null || !_data.Buyable)
                {
                    return;
                }

                Analyzer.Instance.Track("Unity/Shop/IAP/GridCell/Click", ("product-id", _data.Sku()));

                Widget.Find<ShopListPopup>().Show(_data, _purchasingData).Forget();
            });

            L10nManager.OnLanguageChange
                .Subscribe(_ => RefreshLocalized().Forget())
                .AddTo(gameObject);
        }

        public async UniTask RefreshLocalized()
        {
            productName.text = _data.GetNameText();

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
            productImage.sprite = await Util.DownloadTexture($"{MobileShop.MOBILE_L10N_SCHEMA.Host}/{_data.GetListImagePath()}");
            productImage.SetNativeSize();
        }

        public void SetData(InAppPurchaseServiceClient.ProductSchema data, bool isRecommended)
        {
            _data = data;
            _rect = GetComponent<RectTransform>();
            Refresh();
            recommended.SetActive(isRecommended);
        }

        public void SetData(InAppPurchaseServiceClient.ProductSchema data)
        {
            _data = data;
            _rect = GetComponent<RectTransform>();
            Refresh();
        }

        private void Refresh()
        {
            _purchasingData = Game.Game.instance.IAPStoreManager.IAPProducts.FirstOrDefault(p => p.definition.id == _data.Sku());
            if (_purchasingData == null && _data.ProductType == InAppPurchaseServiceClient.ProductType.IAP)
            {
                gameObject.SetActive(false);
                return;
            }

            switch (_data.Size)
            {
                case InAppPurchaseServiceClient.ProductAssetUISize._1x1:
                    _rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 230);
                    bottomButtonLayoutElement.minHeight = 65;
                    bottomLayout.spacing = 0;
                    rewardLayout.gameObject.SetActive(false);
                    break;
                case InAppPurchaseServiceClient.ProductAssetUISize._1x2:
                    _rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 467);
                    bottomButtonLayoutElement.minHeight = 75;
                    bottomLayout.spacing = 3;
                    rewardLayout.gameObject.SetActive(true);

                    var iapRewardIndex = 0;
                    foreach (var item in _data.FavList)
                    {
                        if (iapRewardIndex < rewardViews.Length)
                        {
                            rewardViews[iapRewardIndex].SetFavItem(item);
                            iapRewardIndex++;
                        }
                    }
                    foreach (var item in _data.FungibleItemList)
                    {
                        if (iapRewardIndex < rewardViews.Length)
                        {
                            rewardViews[iapRewardIndex].SetItemBase(item);
                            iapRewardIndex++;
                        }
                    }

                    //4개인 경우 2x2로 배치
                    if(iapRewardIndex == 4)
                    {
                        rewardLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                        rewardLayout.constraintCount = 2;
                    }
                    else
                    {
                        rewardLayout.constraint = GridLayoutGroup.Constraint.Flexible;
                    }

                    for (; iapRewardIndex < rewardViews.Length; iapRewardIndex++)
                    {
                        rewardViews[iapRewardIndex].gameObject.SetActive(false);
                    }

                    break;
            }

            if (_data.Mileage > 0)
            {
                mileageObj.SetActive(true);
                mileageText.text = TextHelper.FormatNumber(_data.Mileage);
            }
            else
            {
                mileageObj.SetActive(false);
            }

            tagObj.SetActive(false);
            discount.gameObject.SetActive(false);
            timeLimitText.gameObject.SetActive(false);
            var isDiscount = _data.Discount > 0;
            foreach (var item in discountObjs)
            {
                item.SetActive(isDiscount);
            }
            switch (_data.ProductType)
            {
                case InAppPurchaseServiceClient.ProductType.IAP:
                    var metadata = _purchasingData?.metadata;
                    NcDebug.Log($"{metadata.localizedTitle} : {metadata.isoCurrencyCode} {metadata.localizedPriceString} {metadata.localizedPrice}");
                    foreach (var item in price)
                    {
                        item.text = MobileShop.GetPrice(metadata.isoCurrencyCode, metadata.localizedPrice);
                    }
                    if (isDiscount)
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
                    break;
                case InAppPurchaseServiceClient.ProductType.FREE:
                    foreach (var item in price)
                    {
                        item.text = L10nManager.Localize("MOBILE_SHOP_PRODUCT_IS_FREE");
                    }
                    _purchasingData = null;
                    break;
                case InAppPurchaseServiceClient.ProductType.MILEAGE:
                    foreach (var item in price)
                    {
                        item.text = L10nManager.Localize("UI_MILEAGE_PRICE", _data.MileagePrice?.ToCurrencyNotation());
                    }
                    _purchasingData = null;
                    break;
            }

            if (_data.DailyLimit != null)
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
            if (_data.ProductType != InAppPurchaseServiceClient.ProductType.FREE)
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
            if (_data == null)
            {
                return 0;
            }

            return _data.Order;
        }
    }
}
