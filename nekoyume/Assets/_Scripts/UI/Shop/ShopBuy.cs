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
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI
{
    public class ShopBuy : Widget
    {
        private const int NPCId = 300000;
        private static readonly Vector2 NPCPosition = new Vector2(2.76f, -1.72f);

        private NPC _npc;

        [SerializeField]
        private CanvasGroup canvasGroup = null;

        [SerializeField]
        private Module.Inventory inventory = null;

        [SerializeField]
        private ShopBuyItems shopItems = null;

        [SerializeField]
        private SpeechBubble speechBubble = null;

        [SerializeField]
        private RefreshButton refreshButton = null;

        private Model.Shop SharedModel { get; set; }

        protected override void Awake()
        {
            base.Awake();
            SharedModel = new Model.Shop();
            CloseWidget = null;
        }

        public override void Initialize()
        {
            base.Initialize();

            inventory.SharedModel.SelectedItemView
                .Subscribe(ShowTooltip)
                .AddTo(gameObject);
            inventory.OnDoubleClickItemView
                .Subscribe(view => ShowActionPopup(view.Model))
                .AddTo(gameObject);
            shopItems.SharedModel.SelectedItemView
                .Subscribe(ShowTooltip)
                .AddTo(gameObject);
            shopItems.SharedModel.OnDoubleClickItemView
                .Subscribe(view => ShowActionPopup(view.Model))
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
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Game.Game.instance.Stage.GetPlayer().gameObject.SetActive(false);
            States.Instance.SetShopState(new ShopState(
                (Bencodex.Types.Dictionary) Game.Game.instance.Agent.GetState(Addresses.Shop)));

            base.Show(ignoreShowAnimation);

            inventory.SharedModel.State.Value = ItemType.Equipment;

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
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            refreshButton.gameObject.SetActive(true);
            canvasGroup.interactable = true;

            var go = Game.Game.instance.Stage.npcFactory.Create(
                NPCId,
                NPCPosition,
                LayerType.InGameBackground,
                3);
            _npc = go.GetComponent<NPC>();
            _npc.SpineController.Appear();
            go.SetActive(true);

            ShowSpeech("SPEECH_SHOP_GREETING_", CharacterAnimation.Type.Greeting);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<ItemCountAndPricePopup>().Close();
            Find<BottomMenu>().Close(ignoreCloseAnimation);
            base.Close(ignoreCloseAnimation);
            _npc?.gameObject.SetActive(false);
        }

        private void ShowTooltip(InventoryItemView view)
        {
            var tooltip = Find<ItemInformationTooltip>();

            shopItems.SharedModel.DeselectItemView();

            if (view is null ||
                view.RectTransform == tooltip.Target)
            {
                tooltip.Close();
                return;
            }

            tooltip.Show(view.RectTransform, view.Model);
        }

        private void ShowTooltip(ShopItemView view)
        {
            var tooltip = Find<ItemInformationTooltip>();
            inventory.SharedModel.DeselectItemView();

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

        private void ShowSpeech(string key,
            CharacterAnimation.Type type = CharacterAnimation.Type.Emotion)
        {
            if (type == CharacterAnimation.Type.Greeting)
            {
                _npc.PlayAnimation(NPCAnimation.Type.Greeting_01);
            }
            else
            {
                _npc.PlayAnimation(NPCAnimation.Type.Emotion_01);
            }

            speechBubble.SetKey(key);
            StartCoroutine(speechBubble.CoShowText());
        }

        private void RefreshAppearAnimation()
        {
            refreshButton.PlayAnimation(NPCAnimation.Type.Appear);
        }
    }
}
