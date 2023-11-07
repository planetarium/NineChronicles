using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nekoyume.UI.Module;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using Nekoyume.Helper;
using Cysharp.Threading.Tasks;
using System.Numerics;
using Nekoyume.L10n;
using Nekoyume.Model.Item;

namespace Nekoyume.UI
{
    public class ShopListPopup : PopupWidget
    {
        [SerializeField]
        private Image productBgImage;

        [SerializeField]
        private GameObject[] discountObjs;
        [SerializeField]
        private TextMeshProUGUI discountText;
        [SerializeField]
        private TextMeshProUGUI[] preDiscountPrice;

        [SerializeField]
        private IAPRewardView[] iapRewards;

        [SerializeField]
        private GameObject buyLimitObj;
        [SerializeField]
        private TextMeshProUGUI buyLimitText;

        [SerializeField]
        private Button buyButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private GameObject buttonDisableObj;

        [SerializeField]
        private GameObject loadIndicator;

        [SerializeField]
        private GameObject buttonActiveEffectObj;

        [SerializeField]
        private TextMeshProUGUI[] priceTexts;

        private ProductSchema _data;
        private UnityEngine.Purchasing.Product _puchasingData;

        protected override void Awake()
        {
            base.Awake();
        
            closeButton.onClick.AddListener(() => {
                Analyzer.Instance.Track("Unity/Shop/IAP/ShopListPopup/Close", ("product-id", _data.Sku));
                Close();
            });
            CloseWidget = () => Close();
            buyButton.onClick.AddListener(() =>
            {
                Debug.Log($"Purchase: {_data.Sku}");
                Analyzer.Instance.Track("Unity/Shop/IAP/ShopListPopup/PurchaseButton/Click",("product-id", _data.Sku));
                Game.Game.instance.IAPStoreManager.OnPurchaseClicked(_data.Sku);

                buyButton.interactable = false;
                buttonDisableObj.SetActive(true);
                loadIndicator.SetActive(true);
                buttonActiveEffectObj.SetActive(false);
            });
        }

        public void PurchaseButtonLoadingEnd()
        {
            buyButton.interactable = true;
            buttonDisableObj.SetActive(false);
            loadIndicator.SetActive(false);
            buttonActiveEffectObj.SetActive(true);
        }

        private async UniTask DownloadTexture()
        {
            productBgImage.sprite = await Util.DownloadTexture($"{MobileShop.MOBILE_L10N_SCHEMA.Host}/{L10nManager.Localize(_data.PopupPathKey)}");
        }

        public async UniTask Show(ProductSchema data, UnityEngine.Purchasing.Product puchasingData,bool ignoreShowAnimation = false)
        {
            _data = data;
            _puchasingData = puchasingData;

            var isDiscount = _data.Discount > 0;

            await DownloadTexture();

            foreach (var item in priceTexts)
            {
                item.text = $"{_puchasingData.metadata.isoCurrencyCode} {_puchasingData.metadata.localizedPrice:N2}";
            }

            int iapRewardIndex = 0;
            for (int i = 0; i < _data.FavList.Length; i++)
            {
                if(iapRewardIndex < iapRewards.Length)
                {
                    iapRewards[iapRewardIndex].gameObject.SetActive(true);
                    iapRewards[iapRewardIndex].RewardImage.sprite = SpriteHelper.GetFavIcon(_data.FavList[i].Ticker.ToString());
                    iapRewards[iapRewardIndex].RewardCount.text = ((BigInteger)_data.FavList[i].Amount).ToCurrencyNotation();
                    iapRewards[iapRewardIndex].RewardGrade.sprite = SpriteHelper.GetItemBackground(Util.GetTickerGrade(_data.FavList[i].Ticker.ToString()));
                    iapRewards[iapRewardIndex].SetItemBase(null);
                    iapRewardIndex++;
                }
            }
            for (int i = 0; i < _data.FungibleItemList.Length; i++)
            {
                if (iapRewardIndex < iapRewards.Length)
                {
                    iapRewards[iapRewardIndex].gameObject.SetActive(true);
                    iapRewards[iapRewardIndex].RewardImage.sprite = SpriteHelper.GetItemIcon(_data.FungibleItemList[i].SheetItemId);
                    iapRewards[iapRewardIndex].RewardCount.text = $"x{_data.FungibleItemList[i].Amount}";
                    try
                    {
                        var itemSheetData = Game.Game.instance.TableSheets.ItemSheet[_data.FungibleItemList[i].SheetItemId];
                        iapRewards[iapRewardIndex].RewardGrade.sprite = SpriteHelper.GetItemBackground(itemSheetData.Grade);
                        var dummyItem = ItemFactory.CreateItem(itemSheetData, new Cheat.DebugRandom());
                        iapRewards[iapRewardIndex].SetItemBase(dummyItem);
                    }
                    catch
                    {
                        Debug.LogError($"Can't Find Item ID {_data.FungibleItemList[i].SheetItemId} in ItemSheet");
                    }
                    
                    iapRewardIndex++;
                }
            }
            for (; iapRewardIndex < iapRewards.Length; iapRewardIndex++)
            {
                iapRewards[iapRewardIndex].gameObject.SetActive(false);
            }

            foreach (var item in discountObjs)
            {
                item.SetActive(isDiscount);
            }

            if (isDiscount)
            {
                discountText.text = _data.Discount.ToString();
                foreach (var item in preDiscountPrice)
                {
                    var originPrice = (_puchasingData.metadata.localizedPrice * ((decimal)100 / (decimal)(100 - _data.Discount)));
                    var origin = $"{_puchasingData.metadata.isoCurrencyCode} {originPrice:N2}";
                    item.text = origin;
                }
            }

            loadIndicator.SetActive(false);
            buyButton.interactable = true;
            buttonDisableObj.SetActive(false);
            buttonActiveEffectObj.SetActive(true);

            buyLimitObj.SetActive(false);
            if (_data.AccountLimit != null)
            {
                buyLimitObj.SetActive(true);
                buyLimitText.text = L10nManager.Localize("MOBILE_SHOP_PRODUCT_AccountLimit", _data.AccountLimit.Value) + $" ({_data.AccountLimit.Value - _data.PurchaseCount}/{_data.AccountLimit.Value})";
            }

            if (_data.WeeklyLimit != null)
            {
                buyLimitObj.SetActive(true);
                buyLimitText.text = L10nManager.Localize("MOBILE_SHOP_PRODUCT_WeeklyLimit", _data.WeeklyLimit.Value) + $" ({_data.WeeklyLimit.Value - _data.PurchaseCount}/{_data.WeeklyLimit.Value})";
            }
            if (_data.DailyLimit != null)
            {
                buyLimitObj.SetActive(true);
                buyLimitText.text = L10nManager.Localize("MOBILE_SHOP_PRODUCT_DailyLimit", _data.DailyLimit.Value) + $" ({_data.DailyLimit.Value - _data.PurchaseCount}/{_data.DailyLimit.Value})";
            }
            Widget.Find<MobileShop>().SetLoadingDataScreen(false);
            base.Show(ignoreShowAnimation);
        }
    }
}
