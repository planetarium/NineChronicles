using System;
using System.Collections.Generic;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ShopRenew : Widget
    {
        public Button switchBuyButton;
        public Button switchSellButton;
        public InventoryAndItemInfo inventoryAndItemInfo;
        public ShopItems shopItems;
        public Button closeButton;
        
        private Model.Shop _data;
        
        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();

        private Stage _stage;
        private Player _player;

        #region Mono

        protected override void Awake()
        {
            base.Awake();
            
            this.ComponentFieldsNotNullTest();
            
            _stage = GameObject.Find("Stage").GetComponent<Stage>();

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
            _player = FindObjectOfType<Player>();
            if (!ReferenceEquals(_player, null))
            {
                _player.gameObject.SetActive(false);
            }
            
            SetData(new Model.Shop(ActionManager.instance.Avatar.Items));
            base.Show();
        }

        public override void Close()
        {
            Clear();
            
            if (!ReferenceEquals(_player, null))
            {
                _player.gameObject.SetActive(true);
            }
            Find<Status>()?.Show();
            Find<Menu>()?.Show();
            base.Close();
        }

        private void SetData(Model.Shop data)
        {
            _data = data;
            _data.state.Value = Model.Shop.State.Buy;
            _data.state.Subscribe(OnState);
            _data.onClickClose.Subscribe(_ => Close());
            
            inventoryAndItemInfo.SetData(_data.inventoryAndItemInfo.Value);
            shopItems.SetData(_data.shopItems.Value);
            
            UpdateView();
        }

        private void Clear()
        {
            shopItems.Clear();
            inventoryAndItemInfo.Clear();
            _data = null;
            
            UpdateView();
        }

        private void UpdateView()
        {
            if (ReferenceEquals(_data, null))
            {
                return;
            }
            
            //
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
        }
    }
}
