using System.Collections.Generic;
using System.Linq;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ShopBuyBoard : MonoBehaviour
    {
        [SerializeField] List<ShopBuyWishItemView> items = new List<ShopBuyWishItemView>();
        [SerializeField] private ShopBuyItems shopItems = null;
        [SerializeField] private GameObject defaultView;
        [SerializeField] private GameObject wishListView;
        [SerializeField] private Button showWishListButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button transactionHistoryButton;

        [SerializeField] private TextMeshProUGUI buyText;
        [SerializeField] private TextMeshProUGUI priceText;

        public readonly Subject<bool> OnChangeBuyType = new Subject<bool>();

        public bool IsAcitveWishListView => wishListView.activeSelf;

        private double _price;

        private void Awake()
        {
            showWishListButton.OnClickAsObservable().Subscribe(ShowWishList).AddTo(gameObject);
            cancelButton.OnClickAsObservable().Subscribe(OnCloseBuyWishList).AddTo(gameObject);
            buyButton.OnClickAsObservable().Subscribe(OnClickBuy).AddTo(gameObject);
            transactionHistoryButton.OnClickAsObservable().Subscribe(OnClickTransactionHistory).AddTo(gameObject);
            buyButton.image.enabled = false;
        }

        private void OnEnable()
        {
            buyButton.image.enabled = true;
            ShowDefaultView();
        }

        private void ShowWishList(Unit unit)
        {
            Clear();
            defaultView.SetActive(false);
            wishListView.SetActive(true);
            OnChangeBuyType.OnNext(true);
        }

        public void ShowDefaultView()
        {
            priceText.text = "0";
            defaultView.SetActive(true);
            wishListView.SetActive(false);
            OnChangeBuyType.OnNext(false);
        }

        private void OnCloseBuyWishList(Unit unit)
        {
            if (shopItems.SharedModel.wishItems.Count > 0)
            {
                Widget.Find<TwoButtonPopup>().Show(L10nManager.Localize("UI_CLOSE_BUY_WISH_LIST"),
                                                   L10nManager.Localize("UI_YES"),
                                                   L10nManager.Localize("UI_NO"), () =>
                                                   {
                                                       ShowDefaultView();
                                                   });
            }
            else
            {
                ShowDefaultView();
            }
        }

        private void OnClickBuy(Unit unit)
        {
            if (_price <= 0)
            {
                return;
            }

            var currentGold = double.Parse(States.Instance.GoldBalanceState.Gold.GetQuantityString());
            if (currentGold < _price)
            {
                OneLinePopup.Push(MailType.System, L10nManager.Localize("UI_NOT_ENOUGH_NCG"));
                return;
            }

            var content = string.Format(L10nManager.Localize("UI_BUY_MULTIPLE_FORMAT"),
                shopItems.SharedModel.wishItems.Count, _price);

            Widget.Find<TwoButtonPopup>().Show(content,
                                               L10nManager.Localize("UI_BUY"),
                                               L10nManager.Localize("UI_CANCEL"),
                                               BuyMultiple);
        }

        private void BuyMultiple()
        {
            var purchaseInfos = shopItems.SharedModel.wishItems.Select(GetPurchseInfo).ToList();
            Game.Game.instance.ActionManager.BuyMultiple(purchaseInfos,
                shopItems.SharedModel.wishItems.ToList());

            foreach (var shopItem in shopItems.SharedModel.wishItems)
            {
                var props = new Value
                {
                    ["Price"] = shopItem.Price.Value.GetQuantityString(),
                };
                Mixpanel.Track("Unity/Buy", props);
                shopItem.Selected.Value = false;
                var buyerAgentAddress = States.Instance.AgentState.address;
                var productId = shopItem.ProductId.Value;

                LocalLayerModifier.ModifyAgentGold(buyerAgentAddress, -shopItem.Price.Value);
                shopItems.SharedModel.RemoveItemSubTypeProduct(productId);
                var format = L10nManager.Localize("NOTIFICATION_BUY_START");
                OneLinePopup.Push(MailType.Auction,
                    string.Format(format, shopItem.ItemBase.Value.GetLocalizedName()));
            }
            AudioController.instance.PlaySfx(AudioController.SfxCode.BuyItem);
            shopItems.SharedModel.ClearWishList();
            UpdateWishList();
        }

        private BuyMultiple.PurchaseInfo GetPurchseInfo(ShopItem shopItem)
        {
            return new BuyMultiple.PurchaseInfo(shopItem.ProductId.Value,
                shopItem.SellerAgentAddress.Value,
                shopItem.SellerAvatarAddress.Value);
        }

        private void OnClickTransactionHistory(Unit unit)
        {
            Widget.Find<Alert>().Show("UI_ALERT_NOT_IMPLEMENTED_TITLE", "UI_ALERT_NOT_IMPLEMENTED_CONTENT");
        }

        private void Clear()
        {
            foreach (var item in items)
            {
                item?.gameObject.SetActive(false);
            }
        }

        public void UpdateWishList()
        {
            Clear();
            _price = 0.0f;
            for (int i = 0; i < shopItems.SharedModel.wishItems.Count; i++)
            {
                var item = shopItems.SharedModel.wishItems[i];
                _price += double.Parse(item.Price.Value.GetQuantityString());
                items[i].gameObject.SetActive(true);
                items[i].SetData(item, () =>
                {
                    shopItems.SharedModel.RemoveItemInWishList(item);
                    UpdateWishList();
                });
            }

            priceText.text = _price.ToString();
            var currentGold = double.Parse(States.Instance.GoldBalanceState.Gold.GetQuantityString());
            if (currentGold < _price)
            {
                priceText.color = Palette.GetColor(3);
                buyButton.image.color = Palette.GetColor(1);
                buyText.color = Palette.GetColor(2);
            }
            else
            {
                priceText.color = Palette.GetColor(0);
                buyButton.image.color = shopItems.SharedModel.wishItems.Count > 0 ? Palette.GetColor(0) : Palette.GetColor(1);
                buyText.color = shopItems.SharedModel.wishItems.Count > 0 ? Palette.GetColor(0) : Palette.GetColor(2);
            }
        }

        private void OnDestroy()
        {
            OnChangeBuyType.Dispose();
        }
    }
}
