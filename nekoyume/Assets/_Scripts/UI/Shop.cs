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

            SetData(new Model.Shop(AvatarManager.Avatar.Items, ReactiveShopState.Items));
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

            Find<Status>()?.Show();
            Find<Menu>()?.Show();
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

            if (_data.state.Value == Model.Shop.State.Buy)
            {
                // ToDo. 구매하겠습니까?
                return;
            }

            if (_data.itemInfo.Value.item.Value is ShopItem)
            {
                // ToDo. 판매 취소하겠습니까?
                return;
            }

            ActionManager.instance
                .Sell(data.item.Value.item.Value.Data.id,
                    data.item.Value.count.Value,
                    data.price.Value)
                .Subscribe(ResponseSell)
                .AddTo(this);
        }

        private void ResponseSell(Nekoyume.Action.Sell.ResultModel result)
        {
            if (ReferenceEquals(result, null))
            {
                throw new GameActionResultNullException();
            }

            if (result.errorCode != GameActionResult.ErrorCode.Success)
            {
                _data.itemCountAndPricePopup.Value.item.Value = null;
                _loadingScreen.Close();
                return;
            }
                    
            if (!Tables.instance.TryGetItemEquipment(result.shopItem.item.Data.id, out var itemEquipment))
            {
                throw new KeyNotFoundException(result.shopItem.item.Data.id.ToString());
            }

            _data.itemCountAndPricePopup.Value.item.Value = null;
            _data.inventory.Value.RemoveItem(result.shopItem.item.Data.id, result.shopItem.count);
            _data.shopItems.Value.registeredProducts.Add(new ShopItem(
                result.owner,
                result.shopItem));
            _loadingScreen.Close();
        }

        private void OnClickCloseItemCountAndPricePopup(Model.ItemCountAndPricePopup data)
        {
            _data.itemCountAndPricePopup.Value.item.Value = null;
            _itemCountAndPricePopup.Close();
        }
    }
}
