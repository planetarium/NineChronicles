using System;
using System.Globalization;
using System.Linq;
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
using Nekoyume.State;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Scroller;

namespace Nekoyume.UI
{
    public class ShopListPopup : PopupWidget
    {
        [SerializeField]
        private Image productBgImage;

        [SerializeField]
        private GameObject[] discountObjs;

        [SerializeField]
        private GameObject tagObj;

        [SerializeField]
        private TextMeshProUGUI discountText;

        [SerializeField]
        private TextMeshProUGUI timeLimitText;

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
        private bool _isInLobby;

        private const string LastReadingDayKey = "SHOP_LIST_POPUP_LAST_READING_DAY";
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

        public bool HasUnread
        {
            get
            {
                var notReadAtToday = true;
                if (PlayerPrefs.HasKey(LastReadingDayKey) &&
                    DateTime.TryParseExact(PlayerPrefs.GetString(LastReadingDayKey),
                        DateTimeFormat, null, DateTimeStyles.None, out var result))
                {
                    notReadAtToday = DateTime.Today != result.Date;
                }

                return notReadAtToday;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                if (_isInLobby)
                {
                    Close();
                    return;
                }

                Analyzer.Instance.Track("Unity/Shop/IAP/ShopListPopup/Close", ("product-id", _data.Sku));

                var evt = new AirbridgeEvent("IAP_ShopListPopup_Close");
                evt.SetAction(_data.Sku);
                evt.AddCustomAttribute("product-id", _data.Sku);
                AirbridgeUnity.TrackEvent(evt);

                Close();
            });
            CloseWidget = () => Close();
            buyButton.onClick.AddListener(() =>
            {
                if (_isInLobby)
                {
                    Close();

                    Find<MobileShop>().ShowAsProduct(_data, _puchasingData);
                    return;
                }

                NcDebug.Log($"Purchase: {_data.Sku}");

                Analyzer.Instance.Track("Unity/Shop/IAP/ShopListPopup/PurchaseButton/Click", ("product-id", _data.Sku));

                var evt = new AirbridgeEvent("IAP_ShopListPopup_PurchaseButton_Click");
                evt.SetAction(_data.Sku);
                evt.AddCustomAttribute("product-id", _data.Sku);
                AirbridgeUnity.TrackEvent(evt);

                if (_data.IsFree)
                {
                    Game.Game.instance.IAPStoreManager.OnPurchaseFreeAsync(_data.Sku).Forget();
                }
                else
                {
                    Game.Game.instance.IAPServiceManager.CheckProductAvailable(_data.Sku, States.Instance.AgentState.address, Game.Game.instance.CurrentPlanetId.ToString(),
                        //success
                        () =>
                        {
                            Game.Game.instance.IAPStoreManager.OnPurchaseClicked(_data.Sku);
                        },
                        //failed
                        () =>
                        {
                            PurchaseButtonLoadingEnd();
                            OneLineSystem.Push(MailType.System,
                                L10nManager.Localize("ERROR_CODE_SHOPITEM_EXPIRED"),
                                NotificationCell.NotificationType.Alert);
                        }).AsUniTask().Forget();
                }

                buyButton.interactable = false;
                buttonDisableObj.SetActive(true);
                loadIndicator.SetActive(true);
                buttonActiveEffectObj.SetActive(false);
                foreach (var item in priceTexts)
                {
                    item.gameObject.SetActive(false);
                }
            });
        }

        public void PurchaseButtonLoadingEnd()
        {
            buyButton.interactable = true;
            buttonDisableObj.SetActive(false);
            loadIndicator.SetActive(false);
            buttonActiveEffectObj.SetActive(true);
            foreach (var item in priceTexts)
            {
                item.gameObject.SetActive(true);
            }
        }

        private async UniTask DownloadTexture()
        {
            productBgImage.sprite = await Util.DownloadTexture($"{MobileShop.MOBILE_L10N_SCHEMA.Host}/{L10nManager.Localize(_data.PopupPathKey)}");
        }

        public async UniTask Show(ProductSchema data, UnityEngine.Purchasing.Product purchasingData, bool ignoreShowAnimation = false)
        {
            _data = data;
            _puchasingData = purchasingData;

            Find<MobileShop>().SetLoadingDataScreen(true);

            await DownloadTexture();

            var metadata = _puchasingData?.metadata;

            if (!_data.IsFree)
            {
                NcDebug.Log($"{metadata.localizedTitle} : {metadata.isoCurrencyCode} {metadata.localizedPriceString} {metadata.localizedPrice}");
                foreach (var item in priceTexts)
                {
                    item.text = MobileShop.GetPrice(metadata.isoCurrencyCode, metadata.localizedPrice);
                }
            }
            else
            {
                foreach (var item in priceTexts)
                {
                    item.text = L10nManager.Localize("MOBILE_SHOP_PRODUCT_IS_FREE");
                }
            }

            // Initialize IAP Reward

            int iapRewardIndex = 0;
            foreach (var item in _data.FavList)
            {
                if (iapRewardIndex < iapRewards.Length)
                {
                    iapRewards[iapRewardIndex].gameObject.SetActive(true);
                    iapRewards[iapRewardIndex].RewardImage.sprite = SpriteHelper.GetFavIcon(item.Ticker);
                    iapRewards[iapRewardIndex].RewardCount.text = ((BigInteger)item.Amount).ToCurrencyNotation();
                    iapRewards[iapRewardIndex].RewardGrade.sprite = SpriteHelper.GetItemBackground(Util.GetTickerGrade(item.Ticker));
                    iapRewards[iapRewardIndex].SetFavItem(item);
                    iapRewardIndex++;
                }
            }

            foreach (var item in _data.FungibleItemList)
            {
                if (iapRewardIndex < iapRewards.Length)
                {
                    iapRewards[iapRewardIndex].gameObject.SetActive(true);
                    iapRewards[iapRewardIndex].RewardImage.sprite = SpriteHelper.GetItemIcon(item.SheetItemId);
                    iapRewards[iapRewardIndex].RewardCount.text = $"x{item.Amount}";
                    try
                    {
                        var itemSheetData = Game.Game.instance.TableSheets.ItemSheet[item.SheetItemId];
                        iapRewards[iapRewardIndex].RewardGrade.sprite = SpriteHelper.GetItemBackground(itemSheetData.Grade);
                        var dummyItem = ItemFactory.CreateItem(itemSheetData, new Cheat.DebugRandom());
                        iapRewards[iapRewardIndex].SetItemBase(dummyItem);
                    }
                    catch
                    {
                        NcDebug.LogError($"Can't Find Item ID {item.SheetItemId} in ItemSheet");
                    }

                    iapRewardIndex++;
                }
            }

            for (; iapRewardIndex < iapRewards.Length; iapRewardIndex++)
            {
                iapRewards[iapRewardIndex].gameObject.SetActive(false);
            }

            //~ Initialize IAP Reward

            var isDiscount = _data.Discount > 0;
            foreach (var item in discountObjs)
            {
                item.SetActive(isDiscount);
            }

            tagObj.SetActive(false);
            discountText.gameObject.SetActive(false);
            timeLimitText.gameObject.SetActive(false);

            if (isDiscount && !_data.IsFree)
            {
                discountText.text = _data.Discount.ToString();
                foreach (var item in preDiscountPrice)
                {
                    var originPrice = metadata.localizedPrice * ((decimal)100 / (100 - _data.Discount));
                    var origin = MobileShop.GetPrice(metadata.isoCurrencyCode, originPrice);
                    item.text = origin;
                }
                tagObj.SetActive(true);
                discountText.gameObject.SetActive(true);
            }

            loadIndicator.SetActive(false);
            foreach (var item in priceTexts)
            {
                item.gameObject.SetActive(true);
            }


            if(_data.RequiredLevel != null)
            {
                buttonDisableObj.SetActive(_data.RequiredLevel > States.Instance.CurrentAvatarState.level);
                buyButton.interactable = !buttonDisableObj.activeSelf;
            }
            else
            {
                buttonDisableObj.SetActive(false);
                buyButton.interactable = true;
            }

            buttonActiveEffectObj.SetActive(true);

            buyLimitObj.SetActive(false);
            if (_data.AccountLimit != null)
            {
                buyLimitObj.SetActive(true);
                buyLimitText.text =
                    $"{L10nManager.Localize("MOBILE_SHOP_PRODUCT_AccountLimit", _data.AccountLimit.Value)} " +
                    $"({_data.AccountLimit.Value - _data.PurchaseCount}/{_data.AccountLimit.Value})";
            }

            if (_data.WeeklyLimit != null)
            {
                buyLimitObj.SetActive(true);
                buyLimitText.text =
                    $"{L10nManager.Localize("MOBILE_SHOP_PRODUCT_WeeklyLimit", _data.WeeklyLimit.Value)} " +
                    $"({_data.WeeklyLimit.Value - _data.PurchaseCount}/{_data.WeeklyLimit.Value})";

                tagObj.SetActive(true);
                timeLimitText.text = MobileShop.RemainTimeForWeeklyLimit;
                timeLimitText.gameObject.SetActive(true);
            }

            if (_data.DailyLimit != null)
            {
                buyLimitObj.SetActive(true);
                buyLimitText.text =
                    $"{L10nManager.Localize("MOBILE_SHOP_PRODUCT_DailyLimit", _data.DailyLimit.Value)} " +
                    $"({_data.DailyLimit.Value - _data.PurchaseCount}/{_data.DailyLimit.Value})";

                tagObj.SetActive(true);
                timeLimitText.text = MobileShop.RemainTimeForDailyLimit;
                timeLimitText.gameObject.SetActive(true);
            }

            Find<MobileShop>().SetLoadingDataScreen(false);
            base.Show(ignoreShowAnimation);

            PlayerPrefs.SetString(LastReadingDayKey, DateTime.Today.ToString(DateTimeFormat));
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _isInLobby = false;
            base.Close(ignoreCloseAnimation);
        }

        public async void ShowAtRoomEntering()
        {
            _isInLobby = true;

            var categorySchemas = await MobileShop.GetCategorySchemas();
            var category = categorySchemas
                .Where(c => c.Active && c.Name != "NoShow")
                .OrderBy(c => c.Order).First();
            var product = category.ProductList
                .Where(p => p.Active && p.Buyable)
                .OrderBy(p => p.Order).First();
            var purchasingProduct = Game.Game.instance.IAPStoreManager.IAPProducts
                .FirstOrDefault(p => p.definition.id == product.Sku);
            Show(product, purchasingProduct).Forget();
        }
    }
}
