using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
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
            if (shopItems.SharedModel.WishItemCount > 0)
            {
                Widget.Find<TwoButtonPopup>().Show(L10nManager.Localize("UI_CLOSE_BUY_WISH_LIST"),
                                                   L10nManager.Localize("UI_YES"),
                                                   L10nManager.Localize("UI_NO"), ShowDefaultView);
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
                shopItems.SharedModel.WishItemCount, _price);

            Widget.Find<TwoButtonPopup>().Show(content,
                                               L10nManager.Localize("UI_BUY"),
                                               L10nManager.Localize("UI_CANCEL"),
                                               BuyMultiple);
        }

        private void BuyMultiple()
        {
            var wishItems = shopItems.SharedModel.GetWishItems;
            var purchaseInfos = new List<PurchaseInfo>();
            purchaseInfos.AddRange(wishItems.Select(x => ShopBuy.GetPurchseInfo(x.OrderId.Value)));
            Game.Game.instance.ActionManager.Buy(purchaseInfos, wishItems);

            if (shopItems.SharedModel.WishItemCount > 0)
            {
                var props = new Value
                {
                    ["Count"] = shopItems.SharedModel.WishItemCount,
                };
                Mixpanel.Track("Unity/Number of Purchased Items", props);
            }

            foreach (var shopItem in shopItems.SharedModel.GetWishItems)
            {
                var props = new Value
                {
                    ["Price"] = shopItem.Price.Value.GetQuantityString(),
                };
                Mixpanel.Track("Unity/Buy", props);
                shopItem.Selected.Value = false;
                var buyerAgentAddress = States.Instance.AgentState.address;
                LocalLayerModifier.ModifyAgentGold(buyerAgentAddress, -shopItem.Price.Value);
                ReactiveShopState.RemoveBuyDigest(shopItem.OrderId.Value);
                var format = L10nManager.Localize("NOTIFICATION_BUY_START");
                OneLinePopup.Push(MailType.Auction,
                    string.Format(format, shopItem.ItemBase.Value.GetLocalizedName()));
            }
            AudioController.instance.PlaySfx(AudioController.SfxCode.BuyItem);
            shopItems.SharedModel.ClearWishList();
            UpdateWishList();
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
            for (int i = 0; i < shopItems.SharedModel.WishItemCount; i++)
            {
                var item = shopItems.SharedModel.GetWishItems[i];
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
                priceText.color = Palette.GetButtonColor(ButtonColorType.Unable);
                buyButton.image.color = Palette.GetButtonColor(ButtonColorType.ColorDisabled);
                buyText.color = Palette.GetButtonColor(ButtonColorType.AlphaDisabled);
            }
            else
            {
                priceText.color = Palette.GetButtonColor(0);
                buyButton.image.color = shopItems.SharedModel.WishItemCount > 0 ?
                    Palette.GetButtonColor(ButtonColorType.Enabled) : Palette.GetButtonColor(ButtonColorType.ColorDisabled);
                buyText.color = shopItems.SharedModel.WishItemCount > 0 ?
                    Palette.GetButtonColor(ButtonColorType.Enabled) : Palette.GetButtonColor(ButtonColorType.AlphaDisabled);
            }
        }

        private void OnDestroy()
        {
            OnChangeBuyType.Dispose();
        }
    }
}
