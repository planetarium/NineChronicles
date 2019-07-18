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
        private Stage _stage;

        private Player _player;
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
            _stage = Game.Game.instance.stage;

            _player = _stage.GetPlayer();
            if (!ReferenceEquals(_player, null))
            {
                _player.gameObject.SetActive(false);
            }

            itemCountAndPricePopup = Find<ItemCountAndPricePopup>();
            if (ReferenceEquals(itemCountAndPricePopup, null))
            {
                throw new NotFoundComponentException<ItemCountAndPricePopup>();
            }

            _loadingScreen = Find<GrayLoadingScreen>();
            if (ReferenceEquals(_loadingScreen, null))
            {
                throw new NotFoundComponentException<LoadingScreen>();
            }
            
            SetData(new Model.Shop(States.Instance.currentAvatarState.Value.inventory, ReactiveShopState.Items));
            base.Show();

            AudioController.instance.PlayMusic(AudioController.MusicCode.Shop);
        }

        public override void Close()
        {
            Clear();

            _stage.GetPlayer(_stage.roomPosition);
            if (!ReferenceEquals(_player, null))
            {
                _player.gameObject.SetActive(true);
            }

            Find<Menu>()?.ShowRoom();
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
            Model.itemInfo.Value.onClick.Subscribe(_ => inventoryAndItemInfo.inventory.Tooltip.Close()).AddTo(_disposablesForModel);
            Model.shopItems.Value.selectedItemView.Subscribe(SubscribeShopItemsSelectedItem)
                .AddTo(_disposablesForModel);
            Model.itemCountAndPricePopup.Value.item.Subscribe(OnPopup).AddTo(_disposablesForModel);
            Model.itemCountAndPricePopup.Value.onClickSubmit.Subscribe(OnClickSubmitItemCountAndPricePopup)
                .AddTo(_disposablesForModel);
            Model.itemCountAndPricePopup.Value.onClickCancel.Subscribe(OnClickCloseItemCountAndPricePopup)
                .AddTo(_disposablesForModel);
            Model.onClickClose.Subscribe(_ => Close()).AddTo(_disposablesForModel);

            inventoryAndItemInfo.SetData(Model.inventory.Value, Model.itemInfo.Value);
            shopItems.SetState(Model.state.Value);
            shopItems.SetData(Model.shopItems.Value);
        }

        private void Clear()
        {
            _sequenceOfShopItems?.Kill();
            bg1.anchoredPosition = new Vector2(_defaultAnchoredPositionXOfBg1, bg1.anchoredPosition.y);
            right.anchoredPosition = new Vector2(_defaultAnchoredPositionXOfRight , right.anchoredPosition.y);
            
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
            if (view is null)
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
            if (view is null)
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
            if (ReferenceEquals(data, null))
            {
                itemCountAndPricePopup.Close();
                return;
            }

            itemCountAndPricePopup.Pop(Model.itemCountAndPricePopup.Value);
        }

        private void OnClickSubmitItemCountAndPricePopup(Model.ItemCountAndPricePopup data)
        {
            AudioController.instance.PlaySfx(AudioController.SfxCode.InputItem);
            _loadingScreen.Show();

            if (Model.itemInfo.Value.item.Value is ShopItem shopItem)
            {
                if (Model.state.Value == UI.Model.Shop.State.Buy)
                {
                    var inventory = States.Instance.currentAvatarState.Value.inventory;
                    // 구매하겠습니다.
                    ActionManager.instance
                        .Buy(shopItem.sellerAgentAddress.Value, shopItem.sellerAvatarAddress.Value,
                            shopItem.productId.Value)
                        .Subscribe(eval =>
                            ResponseBuy(eval, inventory, shopItem.productId.Value, (ItemUsable) shopItem.item.Value))
                        .AddTo(this);
                }
                else
                {
                    // 판매 취소하겠습니다.
                    ActionManager.instance
                        .SellCancellation(shopItem.sellerAvatarAddress.Value, shopItem.productId.Value)
                        .Subscribe(eval =>
                            ResponseSellCancellation(eval, shopItem.productId.Value, (ItemUsable) shopItem.item.Value))
                        .AddTo(this);
                }

                return;
            }

            // 판매하겠습니다.
            ActionManager.instance
                .Sell((ItemUsable) data.item.Value.item.Value, data.price.Value)
                .Subscribe(ResponseSell)
                .AddTo(this);
        }

        private void ResponseSell(ActionBase.ActionEvaluation<Sell> eval)
        {
            Model.itemCountAndPricePopup.Value.item.Value = null;

            var sellerAgentAddress = eval.InputContext.Signer;
            var productId = eval.Action.productId;
            if (!States.Instance.shopState.Value.TryGet(sellerAgentAddress, productId, out var outPair))
            {
                return;
            }

            var shopItem = outPair.Value;

            Model.inventory.Value.RemoveNonFungibleItem(shopItem.itemUsable);

            Model.shopItems.Value.AddShopItem(sellerAgentAddress, shopItem);
            Model.shopItems.Value.AddRegisteredProduct(sellerAgentAddress, shopItem);

            _loadingScreen.Close();
        }

        private void ResponseSellCancellation(ActionBase.ActionEvaluation<SellCancellation> eval, Guid productId,
            ItemUsable shopItem)
        {
            Model.itemCountAndPricePopup.Value.item.Value = null;

            var sellerAgentAddress = eval.InputContext.Signer;

            Model.shopItems.Value.RemoveShopItem(sellerAgentAddress, productId);
            Model.shopItems.Value.RemoveProduct(productId);
            Model.shopItems.Value.RemoveRegisteredProduct(productId);
            Model.inventory.Value.AddNonFungibleItem(shopItem);

            _loadingScreen.Close();
        }

        private void ResponseBuy(ActionBase.ActionEvaluation<Buy> eval, Game.Item.Inventory inventory, Guid productId,
            ItemUsable shopItem)
        {
            Model.itemCountAndPricePopup.Value.item.Value = null;

            var sellerAvatarAddress = eval.InputContext.Signer;

            Model.shopItems.Value.RemoveShopItem(sellerAvatarAddress, productId);
            Model.shopItems.Value.RemoveProduct(productId);
            Model.shopItems.Value.RemoveRegisteredProduct(productId);

            if (!States.Instance.currentAvatarState.Value.inventory.TryGetAddedItemFrom(inventory,
                out _))
            {
                return;
            }

            StartCoroutine(CoShowBuyResultVFX(productId));
            Model.inventory.Value.AddNonFungibleItem(shopItem);
            
            _loadingScreen.Close();
        }

        private IEnumerator CoShowBuyResultVFX(Guid productId)
        {
            var shopItemView = shopItems.GetByProductId(productId);
            if (ReferenceEquals(shopItemView, null))
            {
                yield break;
            }

            yield return new WaitForSeconds(0.1f);

            particleVFX.SetActive(false);
            resultItemVFX.SetActive(false);

            // ToDo. 지금은 구매의 결과가 마지막에 더해지기 때문에 마지막 아이템을 갖고 오지만, 복수의 아이템을 한 번에 얻을 때에 대한 처리나 정렬 기능이 추가 되면 itemGuid로 갖고 와야함.
            var inventoryItem = Model.inventory.Value.items.Last();
            if (ReferenceEquals(inventoryItem, null))
            {
                yield break;
            }

            var index = Model.inventory.Value.items.Count - 1;
            var inventoryItemView = inventoryAndItemInfo.inventory.scrollerController.GetByIndex(index);
            if (ReferenceEquals(inventoryItemView, null))
            {
                yield break;
            }

            particleVFX.transform.position = shopItemView.transform.position;
            particleVFX.transform.DOMoveX(inventoryItemView.transform.position.x, 0.6f);
            particleVFX.transform.DOMoveY(inventoryItemView.transform.position.y, 0.6f).SetEase(Ease.InCubic)
                .onComplete = () => { resultItemVFX.SetActive(true); };
            particleVFX.SetActive(true);
            resultItemVFX.transform.position = inventoryItemView.transform.position;
        }

        private void OnClickCloseItemCountAndPricePopup(Model.ItemCountAndPricePopup data)
        {
            Model.itemCountAndPricePopup.Value.item.Value = null;
            itemCountAndPricePopup.Close();
        }
    }
}
