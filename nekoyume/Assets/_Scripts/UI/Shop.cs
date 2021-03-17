using System;
using System.Linq;
using DG.Tweening;
using Libplanet.Assets;
using mixpanel;
using Nekoyume.BlockChain;
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
using ShopItem = Nekoyume.UI.Model.ShopItem;
using ShopItems = Nekoyume.UI.Module.ShopItems;

namespace Nekoyume.UI
{
    public class Shop : Widget
    {
        public enum StateType
        {
            Show,
            Buy,
            Sell
        }

        private const float GoOutTweenX = 800f;
        private const int NPCId = 300000;
        private static readonly Vector2 NPCPosition = new Vector2(2.76f, -1.72f);

        private bool _isStateShowBefore = false;

        private float _defaultAnchoredPositionXOfBg1;
        private float _defaultAnchoredPositionXOfRight;
        private NPC _npc;

        private Sequence _sequenceOfShopItems;

        private ToggleGroup _toggleGroup;

        [SerializeField]
        private CanvasGroup canvasGroup = null;

        [SerializeField]
        private RectTransform bg1 = null;

        [SerializeField]
        private CategoryButton buyButton = null;

        [SerializeField]
        private CategoryButton sellButton = null;

        [SerializeField]
        private RectTransform right = null;

        [SerializeField]
        private Module.Inventory inventory = null;

        [SerializeField]
        private ShopItems shopItems = null;

        [SerializeField]
        private GameObject shopNotice = null;

        [SerializeField]
        private TextMeshProUGUI noticeText = null;

        [SerializeField]
        private SpeechBubble speechBubble = null;

        [SerializeField]
        private SpriteRenderer sellImage = null;

        [SerializeField]
        private SpriteRenderer buyImage = null;

        [SerializeField]
        private CanvasGroup rightCanvasGroup = null;

        [SerializeField]
        private RefreshButton refreshButton = null;

        private Model.Shop SharedModel { get; set; }

        #region Mono

        protected override void Awake()
        {
            _defaultAnchoredPositionXOfBg1 = bg1.anchoredPosition.x;
            _defaultAnchoredPositionXOfRight = right.anchoredPosition.x;
            base.Awake();

            SharedModel = new Model.Shop();
            noticeText.text = L10nManager.Localize("UI_SHOP_NOTICE");

            CloseWidget = null;
        }

        #endregion

        #region Override

        public override void Initialize()
        {
            base.Initialize();

            _toggleGroup = new ToggleGroup();
            _toggleGroup.OnToggledOn.Subscribe(SubscribeOnToggledOn).AddTo(gameObject);
            _toggleGroup.RegisterToggleable(buyButton);
            _toggleGroup.RegisterToggleable(sellButton);

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

            SharedModel.State.Value = StateType.Show;
            SharedModel.State
                .Subscribe(SubscribeState)
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
            shopItems.SharedModel.State.Value = StateType.Buy;
            SharedModel.State.Value = StateType.Show;

            Find<BottomMenu>().Show(
                UINavigator.NavigationType.Back,
                SubscribeBackButtonClick,
                true,
                BottomMenu.ToggleableType.Mail,
                BottomMenu.ToggleableType.Quest,
                BottomMenu.ToggleableType.Chat,
                BottomMenu.ToggleableType.IllustratedBook,
                BottomMenu.ToggleableType.Character);

            _sequenceOfShopItems = null;

            AudioController.instance.PlayMusic(AudioController.MusicCode.Shop);
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            refreshButton.gameObject.SetActive(true);
            canvasGroup.interactable = true;
            SharedModel.State.Value = StateType.Buy;

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

            _sequenceOfShopItems?.Kill();
            _sequenceOfShopItems = null;
            bg1.anchoredPosition =
                new Vector2(_defaultAnchoredPositionXOfBg1, bg1.anchoredPosition.y);
            right.anchoredPosition =
                new Vector2(_defaultAnchoredPositionXOfRight, right.anchoredPosition.y);
            speechBubble.gameObject.SetActive(false);

            base.Close(ignoreCloseAnimation);

            _npc?.gameObject.SetActive(false);
        }

        #endregion

        #region Subscribe

        private void SubscribeState(StateType stateType)
        {
            Find<ItemInformationTooltip>().Close();
            inventory.SharedModel.DeselectItemView();
            shopItems.SharedModel.DeselectItemView();
            buyButton.SetInteractable(false, true);
            sellButton.SetInteractable(false, true);
            buyImage.gameObject.SetActive(false);
            sellImage.gameObject.SetActive(false);
            switch (stateType)
            {
                case StateType.Show:
                    shopItems.SharedModel.State.Value = stateType;
                    shopNotice.SetActive(false);
                    _toggleGroup.SetToggledOn(buyButton);
                    _isStateShowBefore = true;
                    return;
                case StateType.Buy:
                    inventory.SharedModel.DimmedFunc.Value = null;
                    shopNotice.SetActive(false);
                    _toggleGroup.SetToggledOn(buyButton);

                    if (_isStateShowBefore)
                    {
                        _isStateShowBefore = false;
                        shopItems.SharedModel.State.Value = stateType;
                        buyButton.SetInteractable(false, true);
                        sellButton.SetInteractable(true, true);
                        buyImage.gameObject.SetActive(true);
                        return;
                    }
                    break;
                case StateType.Sell:
                    inventory.SharedModel.DimmedFunc.Value = DimmedFuncForSell;
                    shopNotice.SetActive(true);
                    _toggleGroup.SetToggledOn(sellButton);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stateType), stateType, null);
            }

            canvasGroup.interactable = false;
            rightCanvasGroup.alpha = 0;
            if (_sequenceOfShopItems is null)
            {
                Animator.enabled = false;
                _sequenceOfShopItems = DOTween.Sequence();
                SetSequenceOfShopItems(true, ref _sequenceOfShopItems);
                _sequenceOfShopItems.AppendCallback(() =>
                    shopItems.SharedModel.State.Value = stateType);
                SetSequenceOfShopItems(false, ref _sequenceOfShopItems);
                _sequenceOfShopItems.OnComplete(() =>
                {
                    rightCanvasGroup.DOFade(1f, 0.5f).OnComplete(() =>
                    {
                        Animator.enabled = true;
                        canvasGroup.interactable = true;
                        _sequenceOfShopItems = null;
                        buyButton.SetInteractable(stateType == StateType.Sell, true);
                        sellButton.SetInteractable(stateType == StateType.Buy, true);
                        var isSell = stateType == StateType.Sell;
                        buyImage.gameObject.SetActive(!isSell);
                        sellImage.gameObject.SetActive(isSell);
                    });
                });
            }
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

            if (SharedModel.State.Value == StateType.Buy)
            {
                tooltip.Show(view.RectTransform, view.Model);
            }
            else
            {
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

            if (SharedModel.State.Value == StateType.Buy)
            {
                tooltip.Show(
                    view.RectTransform,
                    view.Model,
                    ButtonEnabledFuncForBuy,
                    L10nManager.Localize("UI_BUY"),
                    _ => ShowBuyPopup(tooltip.itemInformation.Model.item.Value as ShopItem),
                    _ => shopItems.SharedModel.DeselectItemView());
            }
            else
            {
                tooltip.Show(
                    view.RectTransform,
                    view.Model,
                    ButtonEnabledFuncForSell,
                    L10nManager.Localize("UI_RETRIEVE"),
                    _ =>
                        ShowRetrievePopup(tooltip.itemInformation.Model.item.Value as ShopItem),
                    _ => shopItems.SharedModel.DeselectItemView());
            }
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
                    if (SharedModel.State.Value == StateType.Sell)
                    {
                        ShowSellPopup(inventoryItem);
                    }

                    break;
                case ShopItem shopItem:

                    if (SharedModel.State.Value == StateType.Buy)
                    {
                        if (!ButtonEnabledFuncForBuy(shopItem))
                        {
                            return;
                        }
                        ShowBuyPopup(shopItem);
                    }
                    else
                    {
                        ShowRetrievePopup(shopItem);
                    }

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

            if (SharedModel.State.Value == StateType.Buy)
            {
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
            else
            {
                bool exist = shopItems.SharedModel.TryGetShopItemFromAgentProducts(nonFungibleItem.ItemId, out var shopItem);
                if (!exist || shopItem.ExpiredBlockIndex.Value != 0 &&
                    shopItem.ExpiredBlockIndex.Value < Game.Game.instance.Agent.BlockIndex)
                {
                    if (data.Price.Value.Sign * data.Price.Value.MajorUnit < Model.Shop.MinimumPrice)
                    {
                        throw new InvalidSellingPriceException(data);
                    }

                    Game.Game.instance.ActionManager.Sell(
                        (INonFungibleItem)data.Item.Value.ItemBase.Value,
                        data.Price.Value);
                    ResponseSell();

                    return;
                }

                Game.Game.instance.ActionManager.SellCancellation(
                    shopItem.SellerAvatarAddress.Value,
                    shopItem.ProductId.Value);
                ResponseSellCancellation(shopItem);
            }
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

        private void SubscribeOnToggledOn(IToggleable toggleable)
        {
            // NPC Greeting, Emotion 구분을 위해 SubscribeState 외부에서 처리
            if (toggleable.Name.Equals(buyButton.Name))
            {
                SharedModel.State.Value = StateType.Buy;
                ShowSpeech("SPEECH_SHOP_BUY_");
            }
            else if (toggleable.Name.Equals(sellButton.Name))
            {
                SharedModel.State.Value = StateType.Sell;
                ShowSpeech("SPEECH_SHOP_SELL_");
            }
        }

        #endregion

        #region Private Static Methods

        private static bool DimmedFuncForSell(InventoryItem inventoryItem)
        {
            return inventoryItem.ItemBase.Value.ItemType == ItemType.Material;
        }

        private static bool EquippedFuncForSell(InventoryItem inventoryItem)
        {
            switch (inventoryItem.ItemBase.Value)
            {
                case Costume costume:
                    return costume.equipped;
                case Equipment equipment:
                    return equipment.equipped;
                default:
                    return false;
            }
        }

        private static bool ButtonEnabledFuncForBuy(CountableItem inventoryItem)
        {
            return inventoryItem is ShopItem shopItem &&
                   States.Instance.GoldBalanceState.Gold >= shopItem.Price.Value;
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

        #endregion

        #region Response

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

            try
            {
                States.Instance.ShopState.Unregister(productId);
            }
            catch (FailedToUnregisterInShopStateException e)
            {
                Debug.LogError(e.Message);
            }

            shopItems.SharedModel.RemoveAgentProduct(productId);

            AudioController.instance.PlaySfx(AudioController.SfxCode.InputItem);
            var format = L10nManager.Localize("NOTIFICATION_SELL_CANCEL_START");
            Notification.Push(MailType.Auction,
                string.Format(format, shopItem.ItemBase.Value.GetLocalizedName()));
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

        #endregion

        private void SetSequenceOfShopItems(bool isGoOut, ref Sequence sequence)
        {
            var goOutTweenXAbs = Math.Abs(GoOutTweenX);
            sequence.Append(DOTween
                .To(
                    () => bg1.anchoredPosition.x,
                    value =>
                    {
                        var p = bg1.anchoredPosition;
                        p.x = value;
                        bg1.anchoredPosition = p;
                    },
                    isGoOut
                        ? _defaultAnchoredPositionXOfBg1 + GoOutTweenX
                        : _defaultAnchoredPositionXOfBg1,
                    isGoOut
                        ? Math.Abs(goOutTweenXAbs -
                                   Math.Abs(bg1.anchoredPosition.x -
                                            _defaultAnchoredPositionXOfBg1)) /
                          goOutTweenXAbs
                        : Math.Abs(goOutTweenXAbs -
                                   Math.Abs(_defaultAnchoredPositionXOfBg1 -
                                            bg1.anchoredPosition.x)) /
                          goOutTweenXAbs)
                .SetEase(isGoOut ? Ease.InQuint : Ease.OutQuint));
            sequence.Join(DOTween
                .To(
                    () => right.anchoredPosition.x,
                    value =>
                    {
                        var p = right.anchoredPosition;
                        p.x = value;
                        right.anchoredPosition = p;
                    },
                    isGoOut
                        ? _defaultAnchoredPositionXOfRight + GoOutTweenX
                        : _defaultAnchoredPositionXOfRight,
                    isGoOut
                        ? Math.Abs(goOutTweenXAbs -
                                   Math.Abs(right.anchoredPosition.x -
                                            _defaultAnchoredPositionXOfRight)) /
                          goOutTweenXAbs
                        : Math.Abs(goOutTweenXAbs -
                                   Math.Abs(_defaultAnchoredPositionXOfRight -
                                            right.anchoredPosition.x)) /
                          goOutTweenXAbs)
                .SetEase(isGoOut ? Ease.InQuint : Ease.OutQuint));
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
