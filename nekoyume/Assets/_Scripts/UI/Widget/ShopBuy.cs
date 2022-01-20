using System.Collections.Generic;
using System.Threading.Tasks;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI
{
    public class ShopBuy : Widget
    {
        [SerializeField] private Module.ShopBuyItems shopItems = null;
        [SerializeField] private ShopBuyBoard shopBuyBoard = null;
        [SerializeField] private Button sellButton = null;
        [SerializeField] private Button closeButton = null;
        [SerializeField] private Canvas frontCanvas;
        [SerializeField] private List<ShopItemViewRow> itemViewItems;

        private Shop SharedModel { get; set; }

        protected override void Awake()
        {
            base.Awake();
            SharedModel = new Shop();

            var ratio = (float) Screen.height / (float) Screen.width;
            var count = Mathf.RoundToInt(10 * ratio) - 2;

            shopItems.Items.Clear();
            for (int i = 0; i < itemViewItems.Count; i++)
            {
                itemViewItems[i].gameObject.SetActive(i < count);
                if (i < count)
                {
                    shopItems.Items.AddRange(itemViewItems[i].shopItemView);
                }
            }

            sellButton.onClick.AddListener(() =>
            {
                CleanUpWishListAlertPopup(() =>
                {
                    shopItems.Reset();
                    Find<ItemCountAndPricePopup>().Close();
                    Find<ShopSell>().gameObject.SetActive(true);
                    Find<ShopSell>().Show();
                    gameObject.SetActive(false);
                });
            });

            closeButton.onClick.AddListener(() =>
            {
                CleanUpWishListAlertPopup(() =>
                {
                    Close();
                });
            });
            CloseWidget = () => CleanUpWishListAlertPopup(() =>
            {
                Close();
            });
        }

        public override void Initialize()
        {
            base.Initialize();
            shopItems.SharedModel.SelectedItemView
                .Subscribe(OnClickShopItem)
                .AddTo(gameObject);

            SharedModel.ItemCountAndPricePopup.Value.Item
                .Subscribe(SubscribeItemPopup)
                .AddTo(gameObject);

            shopBuyBoard.OnChangeBuyType.Subscribe(SetMultiplePurchase).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            ShowAsync();
        }

        private async void ShowAsync(bool ignoreShowAnimation = false)
        {
            Find<DataLoadingScreen>().Show();
            Game.Game.instance.Stage.GetPlayer().gameObject.SetActive(false);

            var task = Task.Run(async () =>
            {
                await ReactiveShopState.InitAndUpdateBuyDigests();
                return true;
            });

            var result = await task;
            if (result)
            {
                base.Show(ignoreShowAnimation);
                AudioController.instance.PlayMusic(AudioController.MusicCode.Shop);
                shopBuyBoard.ShowDefaultView();
                shopItems.Show();

                Find<ShopSell>().gameObject.SetActive(false);
                Find<DataLoadingScreen>().Close();
                HelpTooltip.HelpMe(100018, true);
            }
        }

        public void Open()
        {
            shopItems.Reset();
            base.Show(true);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (shopItems.IsActiveInputField)
            {
                return;
            }

            shopItems.Close();
            Find<ItemCountAndPricePopup>().Close();
            // This invoking (OnRoomEnter) has dependency with above if statement (shopItems.IsActiveInputField).
            Game.Event.OnRoomEnter.Invoke(true);
            base.Close(true);
        }

        private void ShowTooltip(ShopItemView view)
        {
            var tooltip = Find<ItemInformationTooltip>();

            if (view is null || view.RectTransform == tooltip.Target)
            {
                tooltip.Close();
                return;
            }

            tooltip.ShowForBuy(view.RectTransform, view.Model, ButtonEnabledFuncForBuy,
                L10nManager.Localize("UI_BUY"),
                _ => ShowBuyPopup(tooltip.itemInformation.Model.item.Value as ShopItem),
                _ => shopItems.SharedModel.DeselectItemView());
        }

        private void ShowBuyPopup(ShopItem shopItem)
        {
            if (shopItem is null || shopItem.Dimmed.Value)
            {
                return;
            }

            var price = shopItem.Price.Value.GetQuantityString();
            var content = string.Format(L10nManager.Localize("UI_BUY_MULTIPLE_FORMAT"), 1, price);
            Find<TwoButtonSystem>().Show(content, L10nManager.Localize("UI_BUY"),
                L10nManager.Localize("UI_CANCEL"), (() => { Buy(shopItem); }));
        }

        private void SubscribeItemPopup(CountableItem data)
        {
            if (data is null)
            {
                Find<ItemCountAndPricePopup>().Close();
                return;
            }

            Find<ItemCountAndPricePopup>().Pop(SharedModel.ItemCountAndPricePopup.Value);
        }

        private async void Buy(ShopItem shopItem)
        {
            var purchaseInfos = new List<PurchaseInfo>
            {
                await GetPurchaseInfo(shopItem.OrderId.Value)
            };
            Game.Game.instance.ActionManager.Buy(purchaseInfos).Subscribe();

            var countProps = new Value {["Count"] = 1,};
            Analyzer.Instance.Track("Unity/Number of Purchased Items", countProps);

            var buyProps = new Value {["Price"] = shopItem.Price.Value.GetQuantityString(),};
            Analyzer.Instance.Track("Unity/Buy", buyProps);

            var count = shopItem.Count.Value;
            SharedModel.ItemCountAndPricePopup.Value.Item.Value = null;
            shopItem.Selected.Value = false;

            ReactiveShopState.RemoveBuyDigest(shopItem.OrderId.Value);

            string message;
            if (count > 1)
            {
                message = string.Format(L10nManager.Localize("NOTIFICATION_MULTIPLE_BUY_START"),
                    shopItem.ItemBase.Value.GetLocalizedName(), count);
            }
            else
            {
                message = string.Format(L10nManager.Localize("NOTIFICATION_BUY_START"),
                    shopItem.ItemBase.Value.GetLocalizedName());
            }
            OneLineSystem.Push(MailType.Auction, message, NotificationCell.NotificationType.Information);
            AudioController.instance.PlaySfx(AudioController.SfxCode.BuyItem);
        }

        public void SetMultiplePurchase(bool value)
        {
            shopItems.SharedModel.SetMultiplePurchase(value);
            if (value)
            {
                shopBuyBoard.UpdateWishList();
            }
        }

        private void OnClickClose()
        {
            if (!CanClose)
            {
                Debug.Log(
                    $"Cannot close ShopBuy widget. ShopBuy.CanHandleInputEvent({CanHandleInputEvent}) ShopSell.CanHandleInputEvent({Find<ShopSell>().CanHandleInputEvent})");
                return;
            }

            CleanUpWishListAlertPopup(() =>
            {
                Close();
            });
        }

        private static bool ButtonEnabledFuncForBuy(CountableItem inventoryItem)
        {
            if (!(inventoryItem is ShopItem shopItem))
            {
                return false;
            }

            if (shopItem.ExpiredBlockIndex.Value - Game.Game.instance.Agent.BlockIndex <= 0)
            {
                return false;
            }

            return States.Instance.GoldBalanceState.Gold >= shopItem.Price.Value;
        }

        private void OnClickShopItem(ShopItemView view)
        {
            if (!shopItems.SharedModel.isMultiplePurchase && shopBuyBoard.IsAcitveWishListView)
            {
                SetMultiplePurchase(true);
            }

            if (shopItems.SharedModel.isMultiplePurchase)
            {
                shopBuyBoard.UpdateWishList();
            }
            else
            {
                ShowTooltip(view);
            }
        }

        private void CleanUpWishListAlertPopup(System.Action callback)
        {
            if (shopItems.SharedModel.isMultiplePurchase && shopItems.SharedModel.WishItemCount > 0)
            {
                Widget.Find<TwoButtonSystem>().Show(L10nManager.Localize("UI_CLOSE_BUY_WISH_LIST"),
                    L10nManager.Localize("UI_YES"), L10nManager.Localize("UI_NO"), callback);
            }
            else
            {
                callback.Invoke();
            }
        }

        public static async Task<PurchaseInfo> GetPurchaseInfo(System.Guid orderId)
        {
            var order = await Util.GetOrder(orderId);
            return new PurchaseInfo(orderId, order.TradableId, order.SellerAgentAddress,
                order.SellerAvatarAddress, order.ItemSubType, order.Price);
        }
    }
}
