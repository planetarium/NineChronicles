using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.Serialization;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class EquipmentInventory : MonoBehaviour
    {
        [Serializable]
        private struct CategoryToggle
        {
            public Toggle Toggle;
            public ItemSubType Type;
        }

        [SerializeField] private List<CategoryToggle> categoryToggles = null;
        [SerializeField] private InventoryScroll scroll = null;

        private readonly List<IDisposable> _disposablesAtOnEnable = new List<IDisposable>();
        private readonly Subject<EquipmentInventory> _onResetItems = new Subject<EquipmentInventory>();
        private ItemSubType _itemSubType;

        public Model.EquipmentInventory SharedModel { get; set; }

        protected void Awake()
        {
            foreach (var categoryToggle in categoryToggles)
            {
                categoryToggle.Toggle.onValueChanged.AddListener(value =>
                {
                    if (!value) return;
                    AudioController.PlayClick();
                    SharedModel.State.Value = categoryToggle.Type;
                });
            }

            SharedModel = new Model.EquipmentInventory();
            SharedModel.State.Subscribe(SubscribeState).AddTo(gameObject);
            SharedModel.SelectedItemView.Subscribe(SubscribeSelectedItemView).AddTo(gameObject);

            scroll.OnClick
                .Subscribe(cell => SharedModel.SubscribeItemOnClick(cell.View))
                .AddTo(gameObject);
        }

        private void OnEnable()
        {
            ReactiveAvatarState.Inventory.Subscribe(inventoryState =>
            {
                SharedModel.ResetItems(inventoryState);
                _onResetItems.OnNext(this);
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

        private void SubscribeState(ItemSubType type)
        {
            scroll.UpdateData(SharedModel.Equipments[type], type != _itemSubType);
            _itemSubType = type;

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

        public void ClearItemState(Equipment equipment)
        {
            var item = SharedModel.Equipments[equipment.ItemSubType]
                .FirstOrDefault(x => ((Equipment)x.ItemBase.Value).ItemId.Equals(equipment.ItemId));

            if (!(item is null))
            {
                item.EffectEnabled.Value = false;
                item.Dimmed.Value = false;
            }
        }
    }
}
