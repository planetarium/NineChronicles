using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class Inventory : MonoBehaviour
    {
        [SerializeField]
        private CategoryTabButton equipmentButton = null;

        [SerializeField]
        private CategoryTabButton consumableButton = null;

        [SerializeField]
        private CategoryTabButton materialButton = null;

        [SerializeField]
        private CategoryTabButton costumeButton = null;

        [SerializeField]
        private InventoryScroll scroll = null;

        private ItemType _stateType;

        private readonly ToggleGroup _toggleGroup = new ToggleGroup();

        private readonly List<IDisposable> _disposablesAtOnEnable = new List<IDisposable>();

        public readonly Subject<Inventory> OnResetItems = new Subject<Inventory>();

        public readonly Subject<InventoryItemView> OnDoubleClickItemView =
            new Subject<InventoryItemView>();

        public Model.Inventory SharedModel { get; set; }

        public InventoryScroll Scroll => scroll;

        #region Mono

        protected void Awake()
        {
            _toggleGroup.RegisterToggleable(equipmentButton);
            _toggleGroup.RegisterToggleable(consumableButton);
            _toggleGroup.RegisterToggleable(materialButton);
            _toggleGroup.RegisterToggleable(costumeButton);
            equipmentButton.OnClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                SharedModel.State.Value = ItemType.Equipment;
            }).AddTo(gameObject);
            consumableButton.OnClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                SharedModel.State.Value = ItemType.Consumable;
            }).AddTo(gameObject);
            materialButton.OnClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                SharedModel.State.Value = ItemType.Material;
            }).AddTo(gameObject);
            costumeButton.OnClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                SharedModel.State.Value = ItemType.Costume;
            }).AddTo(gameObject);

            SharedModel = new Model.Inventory();
            SharedModel.State.Subscribe(SubscribeState).AddTo(gameObject);
            SharedModel.SelectedItemView.Subscribe(SubscribeSelectedItemView).AddTo(gameObject);

            scroll.OnClick
                .Subscribe(cell => SharedModel.SubscribeItemOnClick(cell.View))
                .AddTo(gameObject);

            scroll.OnDoubleClick
                .Subscribe(cell =>
                {
                    SharedModel.DeselectItemView();
                    OnDoubleClickItemView.OnNext(cell.View);
                })
                .AddTo(gameObject);
        }

        private void OnEnable()
        {
            ReactiveAvatarState.Inventory.Subscribe(inventoryState =>
            {
                SharedModel.ResetItems(inventoryState);
                OnResetItems.OnNext(this);
            }).AddTo(_disposablesAtOnEnable);
        }

        private void OnDisable()
        {
            _disposablesAtOnEnable.DisposeAllAndClear();

            if (Widget.TryFind<ItemInformationTooltip>(out var tooltip))
            {
                tooltip.Close();
            }
        }

        private void OnDestroy()
        {
            SharedModel.Dispose();
            SharedModel = null;
        }

        #endregion

        #region Subscribe

        private void SubscribeState(ItemType stateType)
        {
            switch (stateType)
            {
                case ItemType.Consumable:
                    if (!consumableButton.IsToggledOn)
                    {
                        _toggleGroup.SetToggledOffAll();
                        consumableButton.SetToggledOn();
                    }
                    scroll.UpdateData(SharedModel.Consumables, stateType != _stateType);
                    break;
                case ItemType.Costume:
                    if (!costumeButton.IsToggledOn)
                    {
                        _toggleGroup.SetToggledOffAll();
                        costumeButton.SetToggledOn();
                    }
                    scroll.UpdateData(SharedModel.Costumes, stateType != _stateType);
                    break;
                case ItemType.Equipment:
                    if (!equipmentButton.IsToggledOn)
                    {
                        _toggleGroup.SetToggledOffAll();
                        equipmentButton.SetToggledOn();
                    }
                    scroll.UpdateData(SharedModel.Equipments, stateType != _stateType);
                    break;
                case ItemType.Material:
                    if (!materialButton.IsToggledOn)
                    {
                        _toggleGroup.SetToggledOffAll();
                        materialButton.SetToggledOn();
                    }
                    scroll.UpdateData(SharedModel.Materials, stateType != _stateType);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stateType), stateType, null);
            }

            _stateType = stateType;

            if (Widget.TryFind<ItemInformationTooltip>(out var tooltip))
            {
                tooltip.Close();
            }
        }

        private void SubscribeSelectedItemView(InventoryItemView view)
        {
            if (view is null)
            {
                return;
            }

            scroll.ScrollTo(view.Model);
        }

        #endregion
    }
}
