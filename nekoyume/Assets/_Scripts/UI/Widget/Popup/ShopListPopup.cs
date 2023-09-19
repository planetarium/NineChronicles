using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nekoyume.UI.Module;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using Nekoyume.Helper;
using System.Numerics;

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
        private TextMeshProUGUI[] priceTexts;

        private ProductSchema _data;
        private UnityEngine.Purchasing.Product _puchasingData;

        protected override void Awake()
        {
            base.Awake();
        
            closeButton.onClick.AddListener(() => { Close(); });
            CloseWidget = () => Close();
            buyButton.onClick.AddListener(() =>
            {
                Debug.Log($"Purchase: {_data.GoogleSku}");
                Analyzer.Instance.Track("Unity/Shop/IAP/PurchaseButton/Click",("product-id", _data.GoogleSku));
                Game.Game.instance.IAPStoreManager.OnPurchaseClicked(_data.GoogleSku);
            });
        }

        public void Show(ProductSchema data, UnityEngine.Purchasing.Product puchasingData,bool ignoreShowAnimation = false)
        {
            _data = data;
            _puchasingData = puchasingData;

            var isDiscount = false;

            foreach (var item in priceTexts)
            {
                item.text = _puchasingData.metadata.localizedPriceString;
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
                    iapRewards[i].gameObject.SetActive(true);
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
                        iapRewards[iapRewardIndex].RewardGrade.sprite = SpriteHelper.GetItemBackground(Game.Game.instance.TableSheets.ItemSheet[_data.FungibleItemList[i].SheetItemId].Grade);
                    }
                    catch
                    {
                        Debug.LogError($"Can't Find Item ID {_data.FungibleItemList[i].SheetItemId} in ItemSheet");
                    }
                    iapRewards[i].gameObject.SetActive(true);
                    iapRewardIndex++;
                }
            }
            for (; iapRewardIndex < iapRewards.Length; iapRewardIndex++)
            {
                iapRewards[iapRewardIndex].gameObject.SetActive(true);
            }

            foreach (var item in discountObjs)
            {
                item.SetActive(isDiscount);
            }

            if (isDiscount)
            {

            }
            else
            {

            }

            base.Show(ignoreShowAnimation);
        }
    }
}
