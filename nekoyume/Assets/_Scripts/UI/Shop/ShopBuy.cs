using System.Collections.Generic;
using mixpanel;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI
{
    public class ShopBuy : Widget
    {
        private const int NPCId = 300000;
        private static readonly Vector2 NPCPosition = new Vector2(2.76f, -1.2f);
        private NPC _npc;

        [SerializeField] private ShopBuyItems shopItems = null;
        [SerializeField] private ShopBuyBoard shopBuyBoard = null;
        [SerializeField] private Button sellButton = null;
        [SerializeField] private Canvas frontCanvas;

        // [SerializeField] private SpeechBubble speechBubble = null;

        private Model.Shop SharedModel { get; set; }

        [SerializeField] private List<ShopItemViewRow> itemViewItems;

        protected override void Awake()
        {
            var ratio = (float)Screen.height / (float)Screen.width;
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

            base.Awake();
            SharedModel = new Model.Shop();
            CloseWidget = null;
            sellButton.onClick.AddListener(() =>
            {
                Find<ShopSell>().Show();
                Find<ShopBuy>().Close();
            });
        }

        public override void Initialize()
        {
            base.Initialize();

            shopItems.SharedModel.SelectedItemView
                .Subscribe(OnClickShopItem)
                .AddTo(gameObject);

            shopItems.SharedModel.OnDoubleClickItemView
                .Subscribe(OnDoubleClickShopItem)
                .AddTo(gameObject);

            SharedModel.ItemCountAndPricePopup.Value.Item
                .Subscribe(SubscribeItemPopup)
                .AddTo(gameObject);
            SharedModel.ItemCountAndPricePopup.Value.OnClickSubmit
                .Subscribe(SubscribeItemPopupSubmit)
                .AddTo(gameObject);
            SharedModel.ItemCountAndPricePopup.Value.OnClickCancel
                .Subscribe(SubscribeItemPopupCancel)
                .AddTo(gameObject);

            shopBuyBoard.OnChangeBuyType.Subscribe(SetMultiplePurchase).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Game.Game.instance.Stage.GetPlayer().gameObject.SetActive(false);
            // States.Instance.SetShopState(new ShopState(
            //     (Bencodex.Types.Dictionary) Game.Game.instance.Agent.GetState(Addresses.Shop)),
            //     shopItems.Items.Count);

            base.Show(ignoreShowAnimation);

            Find<BottomMenu>().Show(
                UINavigator.NavigationType.Back,
                SubscribeBackButtonClick,
                true,
                BottomMenu.ToggleableType.Mail,
                BottomMenu.ToggleableType.Quest,
                BottomMenu.ToggleableType.Chat,
                BottomMenu.ToggleableType.IllustratedBook,
                BottomMenu.ToggleableType.Character);

            AudioController.instance.PlayMusic(AudioController.MusicCode.Shop);
            SetMultiplePurchase(false);

            var go = Game.Game.instance.Stage.npcFactory.Create(
                NPCId,
                NPCPosition,
                LayerType.InGameBackground,
                3);
            _npc = go.GetComponent<NPC>();
            _npc.GetComponent<SortingGroup>().sortingLayerName = LayerType.UI.ToLayerName();
            _npc.GetComponent<SortingGroup>().sortingOrder = 11;
            _npc.SpineController.Appear();

            frontCanvas.sortingLayerName = LayerType.UI.ToLayerName();
            go.SetActive(true);
            shopItems.Show();
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {

            // ShowSpeech("SPEECH_SHOP_GREETING_", CharacterAnimation.Type.Greeting);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<ItemCountAndPricePopup>().Close();
            Find<BottomMenu>().Close(ignoreCloseAnimation);
            base.Close(ignoreCloseAnimation);
            _npc?.gameObject.SetActive(false);
        }

        private void ShowTooltip(ShopItemView view)
        {
            var tooltip = Find<ItemInformationTooltip>();

            if (view is null ||
                view.RectTransform == tooltip.Target)
            {
                tooltip.Close();
                return;
            }

            tooltip.Show(
                view.RectTransform,
                view.Model,
                ButtonEnabledFuncForBuy,
                L10nManager.Localize("UI_BUY"),
                _ => ShowBuyPopup(tooltip.itemInformation.Model.item.Value as ShopItem),
                _ => shopItems.SharedModel.DeselectItemView());
        }

        private void ShowBuyPopup(ShopItem shopItem)
        {
            if (shopItem is null ||
                shopItem.Dimmed.Value)
            {
                return;
            }

            SharedModel.ItemCountAndPricePopup.Value.TitleText.Value =
                L10nManager.Localize("UI_BUY");
            SharedModel.ItemCountAndPricePopup.Value.InfoText.Value =
                L10nManager.Localize("UI_BUY_INFO");
            SharedModel.ItemCountAndPricePopup.Value.CountEnabled.Value = false;
            SharedModel.ItemCountAndPricePopup.Value.Submittable.Value =
                ButtonEnabledFuncForBuy(shopItem);
            SharedModel.ItemCountAndPricePopup.Value.Price.Value = shopItem.Price.Value;
            SharedModel.ItemCountAndPricePopup.Value.PriceInteractable.Value = false;
            SharedModel.ItemCountAndPricePopup.Value.Item.Value = new CountEditableItem(
                shopItem.ItemBase.Value,
                shopItem.Count.Value,
                shopItem.Count.Value,
                shopItem.Count.Value);
        }

        private void ShowActionPopup(CountableItem viewModel)
        {
            if (viewModel is null ||
                viewModel.Dimmed.Value)
                return;

            var shopItem = viewModel as ShopItem;
            if (!ButtonEnabledFuncForBuy(shopItem))
            {
                return;
            }
            ShowBuyPopup(shopItem);
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

        private void AddWishList(ShopItemView view)
        {
            shopBuyBoard.UpdateWishList(shopItems.SharedModel);
        }

        private void SubscribeItemPopupSubmit(Model.ItemCountAndPricePopup data)
        {
            if (!(data.Item.Value.ItemBase.Value is INonFungibleItem nonFungibleItem))
            {
                return;
            }

            if (!shopItems.SharedModel.TryGetShopItemFromItemSubTypeProducts(
                nonFungibleItem.ItemId,
                out var shopItem))
            {
                return;
            }

            var props = new Value
            {
                ["Price"] = shopItem.Price.Value.GetQuantityString(),
            };
            Mixpanel.Track("Unity/Buy", props);

            Game.Game.instance.ActionManager.Buy(
                shopItem.SellerAgentAddress.Value,
                shopItem.SellerAvatarAddress.Value,
                shopItem.ProductId.Value);
            ResponseBuy(shopItem);
            }

        private void SubscribeItemPopupCancel(Model.ItemCountAndPricePopup data)
        {
            SharedModel.ItemCountAndPricePopup.Value.Item.Value = null;
            Find<ItemCountAndPricePopup>().Close();
        }

        private void SetMultiplePurchase(bool value)
        {
            shopItems.SharedModel.SetMultiplePurchase(value);
        }

        private void SubscribeBackButtonClick(BottomMenu bottomMenu)
        {
            if (!CanClose)
            {
                return;
            }

            Close(true);
            Game.Event.OnRoomEnter.Invoke(true);
        }

        private static bool ButtonEnabledFuncForBuy(CountableItem inventoryItem)
        {
            return inventoryItem is ShopItem shopItem &&
                   States.Instance.GoldBalanceState.Gold >= shopItem.Price.Value;
        }

        private void ResponseBuy(ShopItem shopItem)
        {
            SharedModel.ItemCountAndPricePopup.Value.Item.Value = null;

            var buyerAgentAddress = States.Instance.AgentState.address;
            var productId = shopItem.ProductId.Value;

            LocalLayerModifier.ModifyAgentGold(buyerAgentAddress, -shopItem.Price.Value);
            try
            {
                States.Instance.ShopState.Unregister(productId);
            }
            catch (FailedToUnregisterInShopStateException e)
            {
                Debug.LogError(e.Message);
            }
            shopItems.SharedModel.RemoveItemSubTypeProduct(productId);

            AudioController.instance.PlaySfx(AudioController.SfxCode.BuyItem);
            var format = L10nManager.Localize("NOTIFICATION_BUY_START");
            Notification.Push(MailType.Auction,
                string.Format(format, shopItem.ItemBase.Value.GetLocalizedName()));
        }

        private void OnClickShopItem(ShopItemView view)
        {
            if (shopItems.SharedModel.isMultiplePurchase)
            {
                AddWishList(view);
            }
            else
            {
                ShowTooltip(view);
            }
        }

        private void OnDoubleClickShopItem(ShopItemView view)
        {
            if (shopItems.SharedModel.isMultiplePurchase)
            {
                return;
            }

            ShowActionPopup(view.Model);
        }

        // private void ShowSpeech(string key,
        //     CharacterAnimation.Type type = CharacterAnimation.Type.Emotion)
        // {
        //     if (type == CharacterAnimation.Type.Greeting)
        //     {
        //         _npc.PlayAnimation(NPCAnimation.Type.Greeting_01);
        //     }
        //     else
        //     {
        //         _npc.PlayAnimation(NPCAnimation.Type.Emotion_01);
        //     }
        //
        //     speechBubble.SetKey(key);
        //     StartCoroutine(speechBubble.CoShowText());
        // }
    }
}
