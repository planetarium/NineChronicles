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
        [SerializeField] private Button spineButton = null;
        [SerializeField] private Canvas frontCanvas;
        [SerializeField] private List<ShopItemViewRow> itemViewItems;

        private NPC _npc;
        private static readonly Vector3 NPCPosition = new Vector3(1000.1f, 998.2f, 1.7f);
        private const int NPCId = 300000;

        private Shop SharedModel { get; set; }

        protected override void Awake()
        {
            base.Awake();
            SharedModel = new Shop();
            CloseWidget = null;

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
                    _npc?.gameObject.SetActive(false);
                    gameObject.SetActive(false);
                });
            });

            spineButton.onClick.AddListener(() => _npc.PlayAnimation(NPCAnimation.Type.Emotion_01));
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
            AsyncShow();
        }

        private async void AsyncShow(bool ignoreShowAnimation = false)
        {
            Find<DataLoadingScreen>().Show();
            Game.Game.instance.Stage.GetPlayer().gameObject.SetActive(false);

            var task = Task.Run(() =>
            {
                ReactiveShopState.InitBuyDigests();
                return true;
            });

            var result = await task;
            if (result)
            {
                base.Show(ignoreShowAnimation);

                Find<BottomMenu>().Show(UINavigator.NavigationType.Back, SubscribeBackButtonClick,
                    true, BottomMenu.ToggleableType.Mail, BottomMenu.ToggleableType.Quest,
                    BottomMenu.ToggleableType.Chat, BottomMenu.ToggleableType.IllustratedBook,
                    BottomMenu.ToggleableType.Ranking, BottomMenu.ToggleableType.Character);

                AudioController.instance.PlayMusic(AudioController.MusicCode.Shop);
                shopBuyBoard.ShowDefaultView();
                shopItems.Show();

                Reset();
                Find<ShopSell>().gameObject.SetActive(false);
                Find<DataLoadingScreen>().Close();
            }
        }

        private void Reset()
        {
            ShowNPC();
        }

        public void Open()
        {
            shopItems.Reset();
            Reset();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<ItemCountAndPricePopup>().Close();
            Find<BottomMenu>().Close(ignoreCloseAnimation);
            base.Close(ignoreCloseAnimation);
            _npc?.gameObject.SetActive(false);
        }

        public void Close()
        {
            shopItems.Close();
            Close(true);
            Find<ShopSell>().Close();
            Game.Event.OnRoomEnter.Invoke(true);
        }

        private void ShowNPC()
        {
            var go = Game.Game.instance.Stage.npcFactory.Create(NPCId, NPCPosition,
                LayerType.InGameBackground, 3);
            _npc = go.GetComponent<NPC>();
            _npc.SpineController.Appear();
            go.SetActive(true);
            frontCanvas.sortingLayerName = LayerType.UI.ToLayerName();
            _npc.PlayAnimation(NPCAnimation.Type.Greeting_01);
        }

        private void ShowTooltip(ShopItemView view)
        {
            var tooltip = Find<ItemInformationTooltip>();

            if (view is null || view.RectTransform == tooltip.Target)
            {
                tooltip.Close();
                return;
            }

            tooltip.ShowForShop(view.RectTransform, view.Model, ButtonEnabledFuncForBuy,
                L10nManager.Localize("UI_BUY"),
                _ => ShowBuyPopup(tooltip.itemInformation.Model.item.Value as ShopItem),
                _ => shopItems.SharedModel.DeselectItemView(), true);
        }

        private void ShowBuyPopup(ShopItem shopItem)
        {
            if (shopItem is null || shopItem.Dimmed.Value)
            {
                return;
            }

            var price = shopItem.Price.Value.GetQuantityString();
            var content = string.Format(L10nManager.Localize("UI_BUY_MULTIPLE_FORMAT"), 1, price);
            Find<TwoButtonPopup>().Show(content, L10nManager.Localize("UI_BUY"),
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

        private void Buy(ShopItem shopItem)
        {
            var purchaseInfos = new List<PurchaseInfo> {GetPurchseInfo(shopItem.OrderId.Value)};
            Game.Game.instance.ActionManager.Buy(purchaseInfos, new List<ShopItem> {shopItem});

            var countProps = new Value {["Count"] = 1,};
            Mixpanel.Track("Unity/Number of Purchased Items", countProps);

            var buyProps = new Value {["Price"] = shopItem.Price.Value.GetQuantityString(),};
            Mixpanel.Track("Unity/Buy", buyProps);

            SharedModel.ItemCountAndPricePopup.Value.Item.Value = null;
            shopItem.Selected.Value = false;

            var buyerAgentAddress = States.Instance.AgentState.address;

            LocalLayerModifier.ModifyAgentGold(buyerAgentAddress, -shopItem.Price.Value);
            ReactiveShopState.RemoveBuyDigest(shopItem.TradableId.Value);

            var format = L10nManager.Localize("NOTIFICATION_BUY_START");
            OneLinePopup.Push(MailType.Auction,
                string.Format(format, shopItem.ItemBase.Value.GetLocalizedName()));

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

        private void SubscribeBackButtonClick(BottomMenu bottomMenu)
        {
            if (!CanClose)
            {
                Debug.Log(
                    $"Cannot close ShopBuy widget. ShopBuy.CanHandleInputEvent({CanHandleInputEvent}) ShopSell.CanHandleInputEvent({Find<ShopSell>().CanHandleInputEvent})");
                return;
            }

            CleanUpWishListAlertPopup(Close);
        }

        private static bool ButtonEnabledFuncForBuy(CountableItem inventoryItem)
        {
            return inventoryItem is ShopItem shopItem &&
                   States.Instance.GoldBalanceState.Gold >= shopItem.Price.Value;
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
                Widget.Find<TwoButtonPopup>().Show(L10nManager.Localize("UI_CLOSE_BUY_WISH_LIST"),
                    L10nManager.Localize("UI_YES"), L10nManager.Localize("UI_NO"), callback);
            }
            else
            {
                callback.Invoke();
            }
        }

        public static PurchaseInfo GetPurchseInfo(System.Guid orderId)
        {
            var order = Util.GetOrder(orderId);
            return new PurchaseInfo(orderId, order.TradableId, order.SellerAgentAddress,
                order.SellerAvatarAddress, order.ItemSubType, order.Price);
        }
    }
}
