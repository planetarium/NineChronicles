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
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI
{
    public class ShopSell : Widget
    {
        private const int NPCId = 300000;
        private const int ShopItemsPerPage = 20;
        private static readonly Vector2 NPCPosition = new Vector2(2.76f, -1.72f);
        private NPC _npc;

        [SerializeField]
        private Module.Inventory inventory = null;

        [SerializeField]
        private ShopSellItems shopItems = null;

        [SerializeField]
        private TextMeshProUGUI noticeText = null;

        [SerializeField]
        private SpeechBubble speechBubble = null;
        [SerializeField] private Button buyButton = null;

        private Model.Shop SharedModel { get; set; }

        protected override void Awake()
        {
            base.Awake();
            SharedModel = new Model.Shop();
            noticeText.text = L10nManager.Localize("UI_SHOP_NOTICE");
            CloseWidget = null;
            buyButton.onClick.AddListener(() =>
            {
                speechBubble.gameObject.SetActive(false);
                Find<ItemCountAndPricePopup>().Close();
                Find<ShopBuy>().gameObject.SetActive(true);
                Find<ShopBuy>().Open();
                _npc?.gameObject.SetActive(false);
                gameObject.SetActive(false);
            });
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

        public void Show(ShopState shopState, IEnumerable<Nekoyume.Model.Item.ShopItem> shardedProducts)
        {
            base.Show();
            ReactiveShopState.Initialize(shopState, shardedProducts, ShopItemsPerPage);
            shopItems.Show();
            inventory.SharedModel.State.Value = ItemType.Equipment;
            AudioController.instance.PlayMusic(AudioController.MusicCode.Shop);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            shopItems.Close();
            Find<ItemCountAndPricePopup>().Close();
            speechBubble.gameObject.SetActive(false);
            base.Close(ignoreCloseAnimation);
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            var go = Game.Game.instance.Stage.npcFactory.Create(
                NPCId,
                NPCPosition,
                LayerType.InGameBackground,
                3);
            _npc = go.GetComponent<NPC>();
            _npc.GetComponent<SortingGroup>().sortingLayerName = LayerType.InGameBackground.ToLayerName();
            _npc.GetComponent<SortingGroup>().sortingOrder = 3;
            _npc.SpineController.Appear();

            go.SetActive(true);

            ShowSpeech("SPEECH_SHOP_GREETING_", CharacterAnimation.Type.Greeting);
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

            ShowSpeech("SPEECH_SHOP_REGISTER_ITEM_");
            tooltip.Show(
                view.RectTransform,
                view.Model,
                value => !DimmedFuncForSell(value as InventoryItem),
                L10nManager.Localize("UI_SELL"),
                _ =>
                    ShowSellPopup(tooltip.itemInformation.Model.item.Value as InventoryItem),
                _ => inventory.SharedModel.DeselectItemView());
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
                ButtonEnabledFuncForSell,
                L10nManager.Localize("UI_RETRIEVE"),
                _ =>
                    ShowRetrievePopup(tooltip.itemInformation.Model.item.Value as ShopItem),
                _ => shopItems.SharedModel.DeselectItemView());
        }

        private void ShowSellPopup(InventoryItem inventoryItem)
        {
            if (inventoryItem is null ||
                inventoryItem.Dimmed.Value)
            {
                return;
            }

            SharedModel.ItemCountAndPricePopup.Value.TitleText.Value =
                L10nManager.Localize("UI_SELL");
            SharedModel.ItemCountAndPricePopup.Value.InfoText.Value =
                L10nManager.Localize("UI_SELL_INFO");
            SharedModel.ItemCountAndPricePopup.Value.CountEnabled.Value = true;
            SharedModel.ItemCountAndPricePopup.Value.Submittable.Value =
                !DimmedFuncForSell(inventoryItem);
            SharedModel.ItemCountAndPricePopup.Value.PriceInteractable.Value = true;
            SharedModel.ItemCountAndPricePopup.Value.Item.Value = new CountEditableItem(
                inventoryItem.ItemBase.Value,
                1,
                1,
                inventoryItem.Count.Value);
        }

        private void ShowRetrievePopup(ShopItem shopItem)
        {
            if (shopItem is null ||
                shopItem.Dimmed.Value)
            {
                return;
            }

            SharedModel.ItemCountAndPricePopup.Value.TitleText.Value =
                L10nManager.Localize("UI_RETRIEVE");
            SharedModel.ItemCountAndPricePopup.Value.InfoText.Value =
                L10nManager.Localize("UI_RETRIEVE_INFO");
            SharedModel.ItemCountAndPricePopup.Value.CountEnabled.Value = false;
            SharedModel.ItemCountAndPricePopup.Value.Submittable.Value =
                ButtonEnabledFuncForSell(shopItem);
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

            switch (viewModel)
            {
                case InventoryItem inventoryItem:
                    ShowSellPopup(inventoryItem);
                    break;

                case ShopItem shopItem:
                    ShowRetrievePopup(shopItem);
                    break;
            }
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

            if (!shopItems.SharedModel.TryGetShopItemFromAgentProducts(
                nonFungibleItem.ItemId,
                out var shopItem))
            {
                if (data.Price.Value.Sign * data.Price.Value.MajorUnit < Model.Shop.MinimumPrice)
                {
                    throw new InvalidSellingPriceException(data);
                }

                var itemId = ((INonFungibleItem) data.Item.Value.ItemBase.Value).ItemId;
                var itemSubType = data.Item.Value.ItemBase.Value.ItemSubType;
                Game.Game.instance.ActionManager.Sell(itemId, data.Price.Value, itemSubType);
                Mixpanel.Track("Unity/Sell");
                ResponseSell();
                return;
            }

            Mixpanel.Track("Unity/Sell Cancellation");
            Game.Game.instance.ActionManager.SellCancellation(
                shopItem.SellerAvatarAddress.Value,
                shopItem.ProductId.Value,
                shopItem.ItemSubType.Value);
            ResponseSellCancellation(shopItem);
        }

        private void SubscribeItemPopupCancel(Model.ItemCountAndPricePopup data)
        {
            SharedModel.ItemCountAndPricePopup.Value.Item.Value = null;
            Find<ItemCountAndPricePopup>().Close();
        }

        private static bool DimmedFuncForSell(InventoryItem inventoryItem)
        {
            return inventoryItem.ItemBase.Value.ItemType == ItemType.Material;
        }

        private static bool ButtonEnabledFuncForSell(CountableItem inventoryItem)
        {
            switch (inventoryItem)
            {
                case null:
                    return false;
                case ShopItem _:
                    return true;
                default:
                    return !inventoryItem.Dimmed.Value;
            }
        }

        private void ResponseSell()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            var item = SharedModel.ItemCountAndPricePopup.Value.Item.Value;
            SharedModel.ItemCountAndPricePopup.Value.Item.Value = null;

            if (!(item.ItemBase.Value is INonFungibleItem nonFungibleItem))
            {
                return;
            }

            LocalLayerModifier.RemoveItem(avatarAddress, nonFungibleItem.ItemId);
            AudioController.instance.PlaySfx(AudioController.SfxCode.InputItem);
            var format = L10nManager.Localize("NOTIFICATION_SELL_START");
            Notification.Push(MailType.Auction,
                string.Format(format, item.ItemBase.Value.GetLocalizedName()));
        }

        private void ResponseSellCancellation(ShopItem shopItem)
        {
            SharedModel.ItemCountAndPricePopup.Value.Item.Value = null;

            var productId = shopItem.ProductId.Value;

            // try
            // {
            //     States.Instance.ShopState.Unregister(productId);
            // }
            // catch (FailedToUnregisterInShopStateException e)
            // {
            //     Debug.LogError(e.Message);
            // }

            shopItems.SharedModel.RemoveAgentProduct(productId);

            AudioController.instance.PlaySfx(AudioController.SfxCode.InputItem);
            var format = L10nManager.Localize("NOTIFICATION_SELL_CANCEL_START");
            Notification.Push(MailType.Auction,
                string.Format(format, shopItem.ItemBase.Value.GetLocalizedName()));
        }

        private void ShowSpeech(string key,
            CharacterAnimation.Type type = CharacterAnimation.Type.Emotion)
        {
            _npc.PlayAnimation(type == CharacterAnimation.Type.Greeting
                ? NPCAnimation.Type.Greeting_01
                : NPCAnimation.Type.Emotion_01);

            speechBubble.SetKey(key);
        }
    }
}
