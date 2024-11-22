using UnityEngine;
using Cysharp.Threading.Tasks;
using Nekoyume.Model.Item;
using UnityEngine.UI;
using Nekoyume.Game.Controller;
using TMPro;
using System.Linq;
using Nekoyume.ApiClient;
using Nekoyume.State;
using Nekoyume.Model.Mail;
using Nekoyume.L10n;
using Nekoyume.UI.Scroller;

namespace Nekoyume.UI
{
    using UniRx;
    public class SeasonPassPremiumPopup : PopupWidget
    {
        [SerializeField]
        private GameObject[] isPremiumObj;

        [SerializeField]
        private GameObject[] notPremiumObj;

        [SerializeField]
        private GameObject[] isPremiumPlusObj;

        [SerializeField]
        private GameObject[] notPremiumPlusObj;

        [SerializeField]
        private GameObject premiumPurchaseButtonDisabledObj;

        [SerializeField]
        private GameObject premiumPurchaseButtonPriceObj;

        [SerializeField]
        private GameObject premiumPurchaseButtonLoadingObj;

        [SerializeField]
        private GameObject premiumPlusPurchaseButtonDisabledObj;

        [SerializeField]
        private GameObject premiumPlusPurchaseButtonPriceObj;

        [SerializeField]
        private GameObject premiumPlusPurchaseButtonLoadingObj;

        [SerializeField]
        private BaseItemView[] premiumRewards;

        [SerializeField]
        private BaseItemView[] premiumPlusRewards;

        [SerializeField]
        private TextMeshProUGUI[] premiumPrices;

        [SerializeField]
        private TextMeshProUGUI[] premiumPlusPrices;

        [SerializeField]
        private GameObject[] premiumInfoList;

        [SerializeField]
        private GameObject[] premiumPlusInfoList;

        [SerializeField]
        private TextMeshProUGUI titleText;
        [SerializeField]
        private Image premiumIconImage;
        [SerializeField]
        private Image premiumPlusIconImage;

        [SerializeField]
        private Sprite premiumIconCourage;
        [SerializeField]
        private Sprite premiumIconWorldClear;
        [SerializeField]
        private Sprite premiumIconAdventureBoss;
        [SerializeField]
        private Sprite premiumPlusIconCourage;
        [SerializeField]
        private Sprite premiumPlusIconWorldClear;
        [SerializeField]
        private Sprite premiumPlusIconAdventureBoss;

        [SerializeField]
        private GameObject premiumContents;

        [SerializeField]
        private TextMeshProUGUI premiumContentsTitle;
        [SerializeField]
        private TextMeshProUGUI premiumPlusContentsTitle;

        private SeasonPassServiceClient.PassType currentSeasonPassType;

        private enum PassPremiumType
        {
            Premium,
            Premiumplus,
            PremiumAll
        }

        protected override void Awake()
        {
            base.Awake();
        }

        private void RefreshInfoText(string l10nPreText,bool isPlus, GameObject[] infoTexts)
        {
            var infoKeyIndex = 1;
            foreach (var item in infoTexts)
            {
                string l10nKey = $"{l10nPreText}_SEASONPASS_PREMIUM_{(isPlus ? "PLUS_" : "")}INFO_{infoKeyIndex}";
                if (L10nManager.ContainsKey(l10nKey))
                {
                    item.SetActive(true);
                    item.GetComponentInChildren<TextMeshProUGUI>().text = L10nManager.Localize(l10nKey);
                }
                else
                {
                    item.SetActive(false);
                }

                infoKeyIndex++;
            }
        }

        public void Show(SeasonPassServiceClient.PassType seasonPassType, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            currentSeasonPassType = seasonPassType;
            RefreshIcons(ApiClients.Instance.SeasonPassServiceManager.UserSeasonPassDatas[seasonPassType]);
            foreach (var item in premiumRewards)
            {
                item.gameObject.SetActive(false);
            }

            foreach (var item in premiumPlusRewards)
            {
                item.gameObject.SetActive(false);
            }

            var iapStoreManager = Game.Game.instance.IAPStoreManager;

            switch (seasonPassType)
            {
                case SeasonPassServiceClient.PassType.CouragePass:
                    titleText.text = L10nManager.Localize("SEASONPASS_PREMIUM_TITLE_COURAGE");
                    premiumIconImage.sprite = premiumIconCourage;
                    premiumPlusIconImage.sprite = premiumPlusIconCourage;
                    break;
                case SeasonPassServiceClient.PassType.WorldClearPass:
                    titleText.text = L10nManager.Localize("SEASONPASS_PREMIUM_TITLE_WORLDCLEAR");
                    premiumIconImage.sprite = premiumIconWorldClear;
                    premiumPlusIconImage.sprite = premiumPlusIconWorldClear;
                    break;
                case SeasonPassServiceClient.PassType.AdventureBossPass:
                    titleText.text = L10nManager.Localize("SEASONPASS_PREMIUM_TITLE_ADVENTUREBOSS");
                    premiumIconImage.sprite = premiumIconAdventureBoss;
                    premiumPlusIconImage.sprite = premiumPlusIconAdventureBoss;
                    break;
            }

            var premiumProductKey = GetProductKey(seasonPassType, PassPremiumType.Premium);
            var premiumAllProductkey = GetProductKey(seasonPassType, PassPremiumType.PremiumAll);
            var premiumPlusProductkey = GetProductKey(seasonPassType, PassPremiumType.Premiumplus);

            // 프리미엄 상품 하나만있는경우 프리미엄 플러스 컨텐츠에 프리미엄 상품정보를 갱신시킨다.
            if(!iapStoreManager.SeasonPassProduct.ContainsKey(premiumPlusProductkey) && !iapStoreManager.SeasonPassProduct.ContainsKey(premiumAllProductkey))
            {
                //기존 프리미엄 상품설명창을 숨긴다.
                premiumContents.SetActive(false);

                premiumPlusContentsTitle.text = L10nManager.Localize("UI_SEASONPASS_PREMIUM");

                //기존 프리미엄 Plus 상품 설명에 프리미엄키를 세팅한다.
                RefreshInfoText(seasonPassType.ToString().ToUpper(), false, premiumPlusInfoList);
                //기존 프리미엄 Plus 상품정보에 프리미엄 상품정보를 갱신시킨다.
                if (iapStoreManager.SeasonPassProduct.TryGetValue(premiumProductKey, out var premiumProduct))
                {
                    ProcessProduct(premiumProduct, premiumPlusRewards, premiumPlusPrices);
                }
            }
            else
            {
                premiumContents.SetActive(true);

                premiumContentsTitle.text = L10nManager.Localize("UI_SEASONPASS_PREMIUM");
                premiumPlusContentsTitle.text = L10nManager.Localize("UI_SEASONPASS_PREMIUM_PLUS");

                RefreshInfoText(seasonPassType.ToString().ToUpper(), false, premiumInfoList);
                RefreshInfoText(seasonPassType.ToString().ToUpper(), true, premiumPlusInfoList);

                //프리미엄 상품정보갱신
                if (iapStoreManager.SeasonPassProduct.TryGetValue(premiumProductKey, out var premiumProduct))
                {
                    ProcessProduct(premiumProduct, premiumRewards, premiumPrices);
                }

                //프리미엄상태일경우 프리미엄플러스 상품정보갱신 아닐경우 프리미엄ALL 상품갱신
                if (ApiClients.Instance.SeasonPassServiceManager.UserSeasonPassDatas[seasonPassType].IsPremium)
                {
                    if (iapStoreManager.SeasonPassProduct.TryGetValue(premiumPlusProductkey, out var premiumPlusProduct))
                    {
                        ProcessProduct(premiumPlusProduct, premiumPlusRewards, premiumPlusPrices);
                    }
                }
                else
                {
                    if (iapStoreManager.SeasonPassProduct.TryGetValue(premiumAllProductkey, out var premiumAllProduct))
                    {
                        ProcessProduct(premiumAllProduct, premiumPlusRewards, premiumPlusPrices);
                    }
                }
            }
        }

        private void ProcessProduct(InAppPurchaseServiceClient.ProductSchema product, BaseItemView[] rewards, TextMeshProUGUI[] prices)
        {
            var iapStoreManager = Game.Game.instance.IAPStoreManager;
            var index = 0;

            for (var i = 0; i < product.FavList.Count && index < rewards.Length; i++, index++)
            {
                rewards[index].ItemViewSetCurrencyData(product.FavList[i].Ticker, product.FavList[i].Amount);
                AddToolTip(rewards[index], product.FavList[i].Ticker, product.FavList[i].Amount);
            }

            for (var i = 0; i < product.FungibleItemList.Count && index < rewards.Length; i++, index++)
            {
                rewards[index].ItemViewSetItemData(product.FungibleItemList[i].SheetItemId, product.FungibleItemList[i].Amount);
                AddToolTip(rewards[index], product.FungibleItemList[i].SheetItemId);
            }

            var purchasingData = iapStoreManager.IAPProducts.FirstOrDefault(p => p.definition.id == product.Sku());
            if (purchasingData != null)
            {
                foreach (var item in prices)
                {
                    item.text = MobileShop.GetPrice(purchasingData.metadata.isoCurrencyCode, purchasingData.metadata.localizedPrice);
                }
            }
        }

        private void AddToolTip(BaseItemView itemView, int itemId)
        {
            if (itemView.TryGetComponent<SeasonPassPremiumItemView>(out var seasonPassPremiumItemView))
            {
                seasonPassPremiumItemView.TooltipButton.onClick.RemoveAllListeners();
                var itemSheetData = Game.Game.instance.TableSheets.ItemSheet[itemId];
                if (seasonPassPremiumItemView.TooltipButton.onClick.GetPersistentEventCount() < 1)
                {
                    var dummyItem = ItemFactory.CreateItem(itemSheetData, new Cheat.DebugRandom());
                    seasonPassPremiumItemView.TooltipButton.onClick.AddListener(() =>
                    {
                        if (dummyItem == null)
                        {
                            return;
                        }

                        AudioController.PlayClick();
                        var tooltip = ItemTooltip.Find(dummyItem.ItemType);
                        tooltip.Show(dummyItem, string.Empty, false, null);
                    });
                }
            }
        }

        private void AddToolTip(BaseItemView itemView, string ticker, decimal amount)
        {
            if (itemView.TryGetComponent<SeasonPassPremiumItemView>(out var seasonPassPremiumItemView))
            {
                seasonPassPremiumItemView.TooltipButton.onClick.RemoveAllListeners();
                if (seasonPassPremiumItemView.TooltipButton.onClick.GetPersistentEventCount() < 1)
                {
                    seasonPassPremiumItemView.TooltipButton.onClick.AddListener(() =>
                    {
                        Find<FungibleAssetTooltip>().Show(ticker, amount.ToCurrencyNotation(), null);
                    });
                }
            }
        }


        private void RefreshIcons(SeasonPassServiceClient.UserSeasonPassSchema seasonPassInfo)
        {
            if (seasonPassInfo == null)
            {
                return;
            }

            foreach (var item in isPremiumObj)
            {
                item.SetActive(seasonPassInfo.IsPremium);
            }

            foreach (var item in notPremiumObj)
            {
                item.SetActive(!seasonPassInfo.IsPremium);
            }

            premiumPurchaseButtonPriceObj.SetActive(seasonPassInfo.IsPremium);

            foreach (var item in isPremiumPlusObj)
            {
                item.SetActive(seasonPassInfo.IsPremiumPlus);
            }

            foreach (var item in notPremiumPlusObj)
            {
                item.SetActive(!seasonPassInfo.IsPremiumPlus);
            }

            premiumPlusPurchaseButtonPriceObj.SetActive(seasonPassInfo.IsPremiumPlus);
        }

        private void OnPurchase(string productKey)
        {
            ApiClients.Instance.IAPServiceManager.CheckProductAvailable(productKey, States.Instance.AgentState.address, Game.Game.instance.CurrentPlanetId.ToString(),
                //success
                () => { Game.Game.instance.IAPStoreManager.OnPurchaseClicked(productKey); },
                //failed
                () =>
                {
                    PurchaseButtonLoadingEnd();
                    OneLineSystem.Push(MailType.System,
                        L10nManager.Localize("ERROR_CODE_SHOPITEM_EXPIRED"),
                        NotificationCell.NotificationType.Alert);
                }).AsUniTask().Forget();
        }

        public void PurchaseSeasonPassPremiumButton()
        {
            var iapStoreManager = Game.Game.instance.IAPStoreManager;
            var seasonPassManager = ApiClients.Instance.SeasonPassServiceManager;
            if (seasonPassManager.UserSeasonPassDatas[currentSeasonPassType].IsPremium)
            {
                return;
            }

            var productKey = GetProductKey(currentSeasonPassType, PassPremiumType.Premium);

            if (iapStoreManager.SeasonPassProduct.TryGetValue(productKey, out var product))
            {
                premiumPurchaseButtonDisabledObj.SetActive(true);
                premiumPurchaseButtonPriceObj.SetActive(false);
                premiumPurchaseButtonLoadingObj.SetActive(true);
                OnPurchase(product.Sku());
            }
        }

        public void PurchaseSeasonPassPremiumPlusButton()
        {
            var iapStoreManager = Game.Game.instance.IAPStoreManager;
            var seasonPassManager = ApiClients.Instance.SeasonPassServiceManager;
            if (seasonPassManager.UserSeasonPassDatas[currentSeasonPassType].IsPremiumPlus)
            {
                return;
            }

            string productKey;

            var premiumAllProductkey = GetProductKey(currentSeasonPassType, PassPremiumType.PremiumAll);
            var premiumPlusProductkey = GetProductKey(currentSeasonPassType, PassPremiumType.Premiumplus);
            // 프리미엄 상품 하나만있는경우 프리미엄 플러스 컨텐츠에 프리미엄 상품정보를 갱신시킨다.
            if (!iapStoreManager.SeasonPassProduct.ContainsKey(premiumPlusProductkey) && !iapStoreManager.SeasonPassProduct.ContainsKey(premiumAllProductkey))
            {
                productKey = GetProductKey(currentSeasonPassType, PassPremiumType.Premium);
            }
            else if (seasonPassManager.UserSeasonPassDatas[currentSeasonPassType].IsPremium)
            {
                productKey = GetProductKey(currentSeasonPassType, PassPremiumType.Premiumplus);
            }
            else
            {
                productKey = GetProductKey(currentSeasonPassType,PassPremiumType.PremiumAll);
            }

            if (iapStoreManager.SeasonPassProduct.TryGetValue(productKey, out var product))
            {
                premiumPlusPurchaseButtonDisabledObj.SetActive(true);
                premiumPlusPurchaseButtonPriceObj.SetActive(false);
                premiumPlusPurchaseButtonLoadingObj.SetActive(true);
                OnPurchase(product.Sku());
            }
        }

        public void PurchaseButtonLoadingEnd()
        {
            ApiClients.Instance.SeasonPassServiceManager.AvatarStateRefreshAsync().AsUniTask().ContinueWith(() =>
            {
                premiumPurchaseButtonLoadingObj.SetActive(false);
                premiumPlusPurchaseButtonLoadingObj.SetActive(false);
                RefreshIcons(ApiClients.Instance.SeasonPassServiceManager.UserSeasonPassDatas[currentSeasonPassType]);
                if (Find<SeasonPass>().IsActive())
                {
                    Find<SeasonPass>().RefreshCurrentPage();
                }
            });
        }

        private string GetProductKey(SeasonPassServiceClient.PassType seasonPassType, PassPremiumType premiumType)
        {
            var seasonPassManager = ApiClients.Instance.SeasonPassServiceManager;
            var seasonData = seasonPassManager.CurrentSeasonPassData[seasonPassType];

            // 11월 시즌패스의경우 바뀐 상품규칙 전 규칙을 적용하여 상품 검색하도록 예외처리. 이후 다음시즌에선 해당스크립트 제거해야함
            if (seasonPassType == SeasonPassServiceClient.PassType.CouragePass && seasonData.SeasonIndex == 11)
            {
                return $"SeasonPass11{premiumType}";
            }

            return $"{seasonPassType.ToString().ToUpper()}{seasonPassManager.CurrentSeasonPassData[seasonPassType].SeasonIndex}{premiumType}";
        }
    }
}
