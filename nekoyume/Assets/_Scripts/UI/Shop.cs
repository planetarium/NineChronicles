using System;
using System.Collections.Generic;
using Nekoyume.Action;
using Nekoyume.Data;
using Nekoyume.Game.Controller;
using Nekoyume.Model;
using Nekoyume.UI.Model;
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
        public Button switchBuyButton;
        public Button switchSellButton;
        public InventoryAndItemInfo inventoryAndItemInfo;
        public ShopItems shopItems;
        public Button closeButton;

        private Model.Shop _data;

        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        private Stage _stage;
        private Player _player;
        private ItemCountAndPricePopup _itemCountAndPricePopup;
        private GrayLoadingScreen _loadingScreen;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            this.ComponentFieldsNotNullTest();

            switchBuyButton.onClick.AsObservable().Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    _data?.onClickSwitchBuy.OnNext(_data);
                })
                .AddTo(_disposablesForAwake);
            switchSellButton.onClick.AsObservable().Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    _data?.onClickSwitchSell.OnNext(_data);
                })
                .AddTo(_disposablesForAwake);
            closeButton.onClick.AsObservable().Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    _data?.onClickClose.OnNext(_data);
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
            _stage = GameObject.Find("Stage").GetComponent<Stage>();
            if (ReferenceEquals(_stage, null))
            {
                throw new NotFoundComponentException<Stage>();
            }

            _player = _stage.GetPlayer();
            if (!ReferenceEquals(_player, null))
            {
                _player.gameObject.SetActive(false);
            }

            _itemCountAndPricePopup = Find<ItemCountAndPricePopup>();
            if (ReferenceEquals(_itemCountAndPricePopup, null))
            {
                throw new NotFoundComponentException<ItemCountAndPricePopup>();
            }

            _loadingScreen = Find<GrayLoadingScreen>();
            if (ReferenceEquals(_loadingScreen, null))
            {
                throw new NotFoundComponentException<LoadingScreen>();
            }

            SetData(new Model.Shop(States.CurrentAvatar.Value.items, ReactiveShopState.Items));
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

        private void SetData(Model.Shop data)
        {
            _disposablesForSetData.DisposeAllAndClear();
            _data = data;
            _data.state.Value = Model.Shop.State.Buy;
            _data.state.Subscribe(OnState).AddTo(_disposablesForSetData);
            _data.itemCountAndPricePopup.Value.item.Subscribe(OnPopup).AddTo(_disposablesForSetData);
            _data.itemCountAndPricePopup.Value.onClickSubmit.Subscribe(OnClickSubmitItemCountAndPricePopup)
                .AddTo(_disposablesForSetData);
            _data.itemCountAndPricePopup.Value.onClickClose.Subscribe(OnClickCloseItemCountAndPricePopup)
                .AddTo(_disposablesForSetData);
            _data.onClickClose.Subscribe(_ => Close()).AddTo(_disposablesForSetData);

            inventoryAndItemInfo.SetData(_data.inventory.Value, _data.itemInfo.Value);
            shopItems.SetState(_data.state.Value);
            shopItems.SetData(_data.shopItems.Value);
        }

        private void Clear()
        {
            shopItems.Clear();
            inventoryAndItemInfo.Clear();
            _disposablesForSetData.DisposeAllAndClear();
            _data = null;
        }

        private void OnState(Model.Shop.State state)
        {
            switch (state)
            {
                case Model.Shop.State.Buy:
                    switchBuyButton.image.sprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");
                    switchSellButton.image.sprite = Resources.Load<Sprite>("UI/Textures/button_black_01");
                    break;
                case Model.Shop.State.Sell:
                    switchBuyButton.image.sprite = Resources.Load<Sprite>("UI/Textures/button_black_01");
                    switchSellButton.image.sprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");
                    break;
            }
            
            shopItems.SetState(state);
        }

        private void OnPopup(CountableItem data)
        {
            if (ReferenceEquals(data, null))
            {
                _itemCountAndPricePopup.Close();
                return;
            }

            _itemCountAndPricePopup.Pop(_data.itemCountAndPricePopup.Value);
        }

        private void OnClickSubmitItemCountAndPricePopup(Model.ItemCountAndPricePopup data)
        {
            AudioController.instance.PlaySfx(AudioController.SfxCode.InputItem);
            _loadingScreen.Show();

            if (_data.itemInfo.Value.item.Value is ShopItem shopItem)
            {
                if (_data.state.Value == Model.Shop.State.Buy)
                {
                    // 구매하겠습니다.
                    ActionManager.instance
                        .Buy(shopItem.sellerAgentAddress.Value, shopItem.sellerAvatarAddress.Value, shopItem.productId.Value)
                        .Subscribe(ResponseBuy)
                        .AddTo(this);
                }
                else
                {
                    // 판매 취소하겠습니다.
                    ActionManager.instance
                        .SellCancellation(shopItem.sellerAvatarAddress.Value, shopItem.productId.Value)
                        .Subscribe(ResponseSellCancellation)
                        .AddTo(this);
                }

                return;
            }

            // 판매하겠습니다.
            ActionManager.instance
                .Sell(data.item.Value.item.Value.Data.id,
                    data.item.Value.count.Value,
                    data.price.Value)
                .Subscribe(ResponseSell)
                .AddTo(this);
        }

        private void ResponseSell(ActionBase.ActionEvaluation<Action.Sell> eval)
        {
            if (eval.Action.errorCode != GameActionErrorCode.Success)
            {
                _data.itemCountAndPricePopup.Value.item.Value = null;
                _loadingScreen.Close();
                
                // ToDo. 액션 실패 팝업!
                Debug.LogWarning($"액션 실패!! productId: {eval.Action.itemId}");
                
                return;
            }
            
            var result = eval.Action.result;
            if (ReferenceEquals(result, null))
            {
                throw new GameActionResultNullException();
            }
        
            if (!Tables.instance.TryGetItemEquipment(result.shopItem.item.Data.id, out var itemEquipment))
            {
                throw new KeyNotFoundException(result.shopItem.item.Data.id.ToString());
            }

            _data.itemCountAndPricePopup.Value.item.Value = null;
            _data.inventory.Value.RemoveItem(result.shopItem.item.Data.id, result.shopItem.count);
            var registeredProduct = _data.shopItems.Value.AddRegisteredProduct(result.sellerAvatarAddress, result.shopItem);
            _data.shopItems.Value.OnClickShopItem(registeredProduct);
            _loadingScreen.Close();
        }

        private void ResponseSellCancellation(ActionBase.ActionEvaluation<Action.SellCancellation> eval)
        {
            if (eval.Action.errorCode != GameActionErrorCode.Success)
            {
                _data.itemCountAndPricePopup.Value.item.Value = null;
                _loadingScreen.Close();
                
                // ToDo. 액션 실패 팝업!
                Debug.LogWarning($"액션 실패!! productId: {eval.Action.productId}");
                
                return;
            }
            
            var result = eval.Action.result;
            if (ReferenceEquals(result, null))
            {
                throw new GameActionResultNullException();
            }

            _data.itemCountAndPricePopup.Value.item.Value = null;
            _data.shopItems.Value.RemoveProduct(result.shopItem.productId);
            _data.shopItems.Value.RemoveRegisteredProduct(result.shopItem.productId);
            var addedItem = _data.inventory.Value.AddItem(result.shopItem.item, result.shopItem.count);
            _data.inventory.Value.SubscribeOnClick(addedItem);
            _loadingScreen.Close();
        }
        
        private void ResponseBuy(ActionBase.ActionEvaluation<Action.Buy> eval)
        {
            if (eval.Action.errorCode != GameActionErrorCode.Success)
            {
                _data.itemCountAndPricePopup.Value.item.Value = null;
                _loadingScreen.Close();
                
                if (eval.Action.errorCode == GameActionErrorCode.BuySoldOut)
                {
                    // ToDo. 매진 팝업!
                    Debug.LogWarning($"매진!! productId: {eval.Action.productId}");
                    _data.shopItems.Value.RemoveProduct(eval.Action.productId);
                }
                
                return;
            }
            
            var result = eval.Action.result;
            if (ReferenceEquals(result, null))
            {
                throw new GameActionResultNullException();
            }

            _data.itemCountAndPricePopup.Value.item.Value = null;
            _data.shopItems.Value.RemoveProduct(result.shopItem.productId);
            _data.shopItems.Value.RemoveRegisteredProduct(result.shopItem.productId);
            var addedItem = _data.inventory.Value.AddItem(result.shopItem.item, result.shopItem.count);
            _data.inventory.Value.SubscribeOnClick(addedItem);
            _loadingScreen.Close();
        }
        
        private void OnClickCloseItemCountAndPricePopup(Model.ItemCountAndPricePopup data)
        {
            _data.itemCountAndPricePopup.Value.item.Value = null;
            _itemCountAndPricePopup.Close();
        }
    }
}
