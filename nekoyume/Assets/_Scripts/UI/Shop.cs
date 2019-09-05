using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using DG.Tweening;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using InventoryAndItemInfo = Nekoyume.UI.Module.InventoryAndItemInfo;
using Player = Nekoyume.Game.Character.Player;
using ShopItem = Nekoyume.UI.Model.ShopItem;
using ShopItems = Nekoyume.UI.Module.ShopItems;
using Stage = Nekoyume.Game.Stage;

namespace Nekoyume.UI
{
    public class Shop : Widget
    {
        public CanvasGroup canvasGroup;
        public RectTransform bg1;
        public RectTransform right;
        public Button switchBuyButton;
        public Text switchBuyButtonText;
        public Button switchSellButton;
        public Text switchSellButtonText;
        public Text catQuoteText;
        public InventoryAndItemInfo inventoryAndItemInfo;
        public ShopItems shopItems;
        public Button closeButton;
        public ItemCountAndPricePopup itemCountAndPricePopup;


        public GameObject particleVFX;
        public GameObject resultItemVFX;

        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        private float _defaultAnchoredPositionXOfBg1;
        private float _defaultAnchoredPositionXOfRight;
        private float _goOutTweenX = 800f;

        private GrayLoadingScreen _loadingScreen;

        private Sequence _sequenceOfShopItems;

        public Model.Shop Model { get; private set; }

        #region Mono

        protected override void Awake()
        {
            _defaultAnchoredPositionXOfBg1 = bg1.anchoredPosition.x;
            _defaultAnchoredPositionXOfRight = right.anchoredPosition.x;
            base.Awake();

            switchBuyButtonText.text = LocalizationManager.Localize("UI_BUY");
            switchSellButtonText.text = LocalizationManager.Localize("UI_SELL");
            catQuoteText.text = LocalizationManager.Localize("SPEECH_SHOP_0");

            switchBuyButton.onClick.AsObservable().Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Model?.onClickSwitchBuy.OnNext(Model);
                })
                .AddTo(_disposablesForAwake);
            switchSellButton.onClick.AsObservable().Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Model?.onClickSwitchSell.OnNext(Model);
                })
                .AddTo(_disposablesForAwake);
            closeButton.onClick.AsObservable().Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Model?.onClickClose.OnNext(Model);
                })
                .AddTo(_disposablesForAwake);
        }

        private void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
            Clear();
        }

        #endregion

        public override void Show()
        {
            var stage = Game.Game.instance.stage;
            var player = stage.GetPlayer();
            if (player)
            {
                player.gameObject.SetActive(false);
            }

            itemCountAndPricePopup = Find<ItemCountAndPricePopup>();
            if (!itemCountAndPricePopup)
            {
                throw new NotFoundComponentException<ItemCountAndPricePopup>();
            }

            _loadingScreen = Find<GrayLoadingScreen>();
            if (!_loadingScreen)
            {
                throw new NotFoundComponentException<LoadingScreen>();
            }

            SetData(new Model.Shop(States.Instance.currentAvatarState.Value.inventory, ReactiveShopState.Items));
            base.Show();

            AudioController.instance.PlayMusic(AudioController.MusicCode.Shop);
        }

        public override void OnCompleteOfShowAnimation()
        {
            base.OnCompleteOfShowAnimation();
            canvasGroup.interactable = true;
        }

        public override void Close()
        {
            Clear();

            base.Close();

            AudioController.instance.PlayMusic(AudioController.MusicCode.Main);
        }

        private void SetData(Model.Shop model)
        {
            _disposablesForModel.DisposeAllAndClear();
            Model = model;
            Model.state.Value = UI.Model.Shop.State.Show;
            Model.state.Subscribe(SubscribeState).AddTo(_disposablesForModel);
            Model.inventory.Value.selectedItemView.Subscribe(SubscribeInventorySelectedItem)
                .AddTo(_disposablesForModel);
            Model.itemInfo.Value.onClick.Subscribe(_ => inventoryAndItemInfo.inventory.Tooltip.Close())
                .AddTo(_disposablesForModel);
            Model.shopItems.Value.selectedItemView.Subscribe(SubscribeShopItemsSelectedItem)
                .AddTo(_disposablesForModel);
            Model.itemCountAndPricePopup.Value.item.Subscribe(OnPopup).AddTo(_disposablesForModel);
            Model.itemCountAndPricePopup.Value.onClickSubmit.Subscribe(OnClickSubmitItemCountAndPricePopup)
                .AddTo(_disposablesForModel);
            Model.itemCountAndPricePopup.Value.onClickCancel.Subscribe(OnClickCloseItemCountAndPricePopup)
                .AddTo(_disposablesForModel);
            Model.onClickClose.Subscribe(_ => GoToMenu()).AddTo(_disposablesForModel);

            inventoryAndItemInfo.SetData(Model.inventory.Value, Model.itemInfo.Value);
            shopItems.SetState(Model.state.Value);
            shopItems.SetData(Model.shopItems.Value);
        }

        private void Clear()
        {
            _sequenceOfShopItems?.Kill();
            bg1.anchoredPosition = new Vector2(_defaultAnchoredPositionXOfBg1, bg1.anchoredPosition.y);
            right.anchoredPosition = new Vector2(_defaultAnchoredPositionXOfRight, right.anchoredPosition.y);

            shopItems.Clear();
            inventoryAndItemInfo.Clear();
            _disposablesForModel.DisposeAllAndClear();
            Model = null;
        }

        private void SubscribeState(Model.Shop.State state)
        {
            switch (state)
            {
                case UI.Model.Shop.State.Show:
                    shopItems.SetState(state);
                    Model.state.Value = UI.Model.Shop.State.Buy;
                    switchBuyButton.image.sprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");
                    switchSellButton.image.sprite = Resources.Load<Sprite>("UI/Textures/button_black_01");
                    return;
                case UI.Model.Shop.State.Buy:
                    switchBuyButton.image.sprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");
                    switchSellButton.image.sprite = Resources.Load<Sprite>("UI/Textures/button_black_01");
                    break;
                case UI.Model.Shop.State.Sell:
                    switchBuyButton.image.sprite = Resources.Load<Sprite>("UI/Textures/button_black_01");
                    switchSellButton.image.sprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }

            if (inventoryAndItemInfo.inventory.Tooltip)
            {
                inventoryAndItemInfo.inventory.Tooltip.Close();
            }

            canvasGroup.interactable = false;
            _sequenceOfShopItems?.Kill();
            _sequenceOfShopItems = DOTween.Sequence();
            SetSequenceOfShopItems(true, ref _sequenceOfShopItems);
            _sequenceOfShopItems.AppendCallback(() => shopItems.SetState(state));
            SetSequenceOfShopItems(false, ref _sequenceOfShopItems);
            _sequenceOfShopItems.OnComplete(() => canvasGroup.interactable = true);
        }

        private void SetSequenceOfShopItems(bool isGoOut, ref Sequence sequence)
        {
            var goOutTweenXAbs = Math.Abs(_goOutTweenX);
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
                        ? _defaultAnchoredPositionXOfBg1 + _goOutTweenX
                        : _defaultAnchoredPositionXOfBg1,
                    isGoOut
                        ? Math.Abs(goOutTweenXAbs - Math.Abs(bg1.anchoredPosition.x - _defaultAnchoredPositionXOfBg1)) /
                          goOutTweenXAbs
                        : Math.Abs(goOutTweenXAbs - Math.Abs(_defaultAnchoredPositionXOfBg1 - bg1.anchoredPosition.x)) /
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
                        ? _defaultAnchoredPositionXOfRight + _goOutTweenX
                        : _defaultAnchoredPositionXOfRight,
                    isGoOut
                        ? Math.Abs(goOutTweenXAbs -
                                   Math.Abs(right.anchoredPosition.x - _defaultAnchoredPositionXOfRight)) /
                          goOutTweenXAbs
                        : Math.Abs(goOutTweenXAbs -
                                   Math.Abs(_defaultAnchoredPositionXOfRight - right.anchoredPosition.x)) /
                          goOutTweenXAbs)
                .SetEase(isGoOut ? Ease.InQuint : Ease.OutQuint));
        }

        private void SubscribeInventorySelectedItem(InventoryItemView view)
        {
            if (!view)
            {
                return;
            }

            if (inventoryAndItemInfo.inventory.Tooltip.Model.target.Value == view.RectTransform)
            {
                inventoryAndItemInfo.inventory.Tooltip.Close();

                return;
            }

            if (Model.state.Value == UI.Model.Shop.State.Buy)
            {
                inventoryAndItemInfo.inventory.Tooltip.Show(
                    view.RectTransform,
                    view.Model);
            }
            else
            {
                inventoryAndItemInfo.inventory.Tooltip.Show(
                    view.RectTransform,
                    view.Model,
                    value => !Model.DimmedFuncForSell(view.Model),
                    LocalizationManager.Localize("UI_SELL"),
                    tooltip =>
                    {
                        Model.OnClickItemInfo(tooltip.itemInformation.Model.item.Value);
                        inventoryAndItemInfo.inventory.Tooltip.Close();
                    });
            }
        }

        private void SubscribeShopItemsSelectedItem(ShopItemView view)
        {
            if (!view)
            {
                return;
            }

            if (inventoryAndItemInfo.inventory.Tooltip.Model.target.Value == view.RectTransform)
            {
                inventoryAndItemInfo.inventory.Tooltip.Close();

                return;
            }

            if (Model.state.Value == UI.Model.Shop.State.Buy)
            {
                inventoryAndItemInfo.inventory.Tooltip.Show(
                    view.RectTransform,
                    view.Model,
                    value => Model.ButtonEnabledFuncForBuy(view.Model),
                    LocalizationManager.Localize("UI_BUY"),
                    tooltip =>
                    {
                        Model.OnClickItemInfo(tooltip.itemInformation.Model.item.Value);
                        inventoryAndItemInfo.inventory.Tooltip.Close();
                    });
            }
            else
            {
                inventoryAndItemInfo.inventory.Tooltip.Show(
                    view.RectTransform,
                    view.Model,
                    value => Model.ButtonEnabledFuncForSell(view.Model),
                    LocalizationManager.Localize("UI_RETRIEVE"),
                    tooltip =>
                    {
                        Model.OnClickItemInfo(tooltip.itemInformation.Model.item.Value);
                        inventoryAndItemInfo.inventory.Tooltip.Close();
                    });
            }
        }

        private void OnPopup(CountableItem data)
        {
            if (data is null)
            {
                itemCountAndPricePopup.Close();
                return;
            }

            itemCountAndPricePopup.Pop(Model.itemCountAndPricePopup.Value);
        }

        private void OnClickSubmitItemCountAndPricePopup(Model.ItemCountAndPricePopup data)
        {
            if (Model.itemInfo.Value.item.Value is ShopItem shopItem)
            {
                if (Model.state.Value == UI.Model.Shop.State.Buy)
                {
                    // 구매하겠습니다.
                    ActionManager.instance
                        .Buy(shopItem.sellerAgentAddress.Value, shopItem.sellerAvatarAddress.Value,
                            shopItem.productId.Value);
                    ResponseBuy(shopItem);
                }
                else
                {
                    // 판매 취소하겠습니다.
                    ActionManager.instance.SellCancellation(shopItem.sellerAvatarAddress.Value, shopItem.productId.Value);
                    ResponseSellCancellation(shopItem);
                }

                return;
            }

            // 판매하겠습니다.
            ActionManager.instance.Sell((ItemUsable) data.item.Value.item.Value, data.price.Value);
            ResponseSell();
        }

        private void ResponseSell()
        {
            var item = Model.itemCountAndPricePopup.Value.item.Value;
            Model.inventory.Value.RemoveItem(item.item.Value);
            Model.itemCountAndPricePopup.Value.item.Value = null;
            AudioController.instance.PlaySfx(AudioController.SfxCode.InputItem);
            Notification.Push($"{item.item.Value.Data.name} 아이템을 상점에 등록합니다.");
        }

        private void ResponseSellCancellation(ShopItem shopItem)
        {
            Model.itemCountAndPricePopup.Value.item.Value = null;

            var sellerAgentAddress = shopItem.sellerAgentAddress.Value;
            var productId = shopItem.productId.Value;

            Model.shopItems.Value.RemoveShopItem(sellerAgentAddress, productId);
            Model.shopItems.Value.RemoveProduct(productId);
            Model.shopItems.Value.RemoveRegisteredProduct(productId);
            AudioController.instance.PlaySfx(AudioController.SfxCode.InputItem);
            Notification.Push($"{shopItem.item.Value.Data.name} 아이템을 판매 취소합니다.");
        }

        private void ResponseBuy(ShopItem shopItem)
        {
            Model.itemCountAndPricePopup.Value.item.Value = null;
            var productId = shopItem.productId.Value;
            Model.shopItems.Value.RemoveShopItem(shopItem.sellerAvatarAddress.Value, productId);
            Model.shopItems.Value.RemoveProduct(productId);
            Model.shopItems.Value.RemoveRegisteredProduct(productId);
            AudioController.instance.PlaySfx(AudioController.SfxCode.BuyItem);
            Notification.Push($"{shopItem.item.Value.Data.name} 아이템을 구매합니다.");
        }

        private void OnClickCloseItemCountAndPricePopup(Model.ItemCountAndPricePopup data)
        {
            Model.itemCountAndPricePopup.Value.item.Value = null;
            itemCountAndPricePopup.Close();
        }

        private void GoToMenu()
        {
            Close();
            Find<Menu>().ShowRoom();
        }
    }
}
