using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UnityEngine;
using Material = Nekoyume.Model.Item.Material;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class InventoryView : MonoBehaviour
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
        private InventoryViewScroll scroll = null;

        private readonly ReactiveCollection<InventoryItemViewModel> _equipments =
            new ReactiveCollection<InventoryItemViewModel>();

        private readonly ReactiveCollection<InventoryItemViewModel> _consumables =
            new ReactiveCollection<InventoryItemViewModel>();

        private readonly ReactiveCollection<InventoryItemViewModel> _materials =
            new ReactiveCollection<InventoryItemViewModel>();

        private readonly ReactiveCollection<InventoryItemViewModel> _costumes =
            new ReactiveCollection<InventoryItemViewModel>();

        private readonly ToggleGroup _toggleGroup = new ToggleGroup();

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private InventoryItemViewModel _selectedItem;

        private Action<InventoryItemViewModel, RectTransform> _onClickItem;
        private Action<InventoryItemViewModel> _onDoubleClickItem;
        private System.Action _onToggleEquipment;
        private System.Action _onToggleCostume;
        private System.Action _onToggleConsumable;
        private ItemType _activeItemType = ItemType.Equipment;

        public bool HasNotification => _equipments.Any(x => x.HasNotification.Value);

        protected void Awake()
        {
            _toggleGroup.RegisterToggleable(equipmentButton);
            _toggleGroup.RegisterToggleable(consumableButton);
            _toggleGroup.RegisterToggleable(materialButton);
            _toggleGroup.RegisterToggleable(costumeButton);

            equipmentButton.OnClick.Subscribe(_ =>
                {
                    SetToggle(equipmentButton, ItemType.Equipment);
                    _onToggleEquipment?.Invoke();
                })
                .AddTo(gameObject);
            costumeButton.OnClick.Subscribe(_ =>
                {
                    SetToggle(costumeButton, ItemType.Costume);
                    _onToggleCostume?.Invoke();
                })
                .AddTo(gameObject);
            consumableButton.OnClick
                .Subscribe(_ =>
                {
                    SetToggle(consumableButton, ItemType.Consumable);
                    _onToggleConsumable?.Invoke();
                })
                .AddTo(gameObject);
            materialButton.OnClick.Subscribe(_ => SetToggle(materialButton, ItemType.Material))
                .AddTo(gameObject);
        }

        public void OnEnable()
        {
            _disposables.DisposeAllAndClear();
            ReactiveAvatarState.Inventory.Subscribe(inventory =>
            {
                DisposeModels(_equipments);
                DisposeModels(_consumables);
                DisposeModels(_materials);
                DisposeModels(_costumes);

                if (inventory is null)
                {
                    return;
                }

                _selectedItem = null;
                foreach (var item in
                         inventory.Items.OrderByDescending(x => x.item is ITradableItem))
                {
                    if (item.Locked)
                    {
                        continue;
                    }

                    AddItem(item.item, item.count);
                }

                scroll.UpdateData(GetModels(_activeItemType), false);
                UpdateEquipmentNotification();
            }).AddTo(_disposables);

            SetToggle(equipmentButton, ItemType.Equipment);
            scroll.OnClick.Subscribe(OnClickItem).AddTo(_disposables);
            scroll.OnDoubleClick.Subscribe(OnDoubleClick).AddTo(_disposables);
        }

        private void DisposeModels(ReactiveCollection<InventoryItemViewModel> models)
        {
            foreach (var model in models)
            {
                model.Dispose();
            }

            models.Clear();
        }

        private void SetToggle(IToggleable toggle, ItemType itemType)
        {
            _activeItemType = itemType;
            scroll.UpdateData(GetModels(itemType), !toggle.IsToggledOn);
            DisableFocus();
            _toggleGroup.SetToggledOffAll();
            toggle.SetToggledOn();
            AudioController.PlayClick();
        }

        private void AddItem(ItemBase itemBase, int count = 1)
        {
            if (itemBase is ITradableItem tradableItem)
            {
                var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;
                if (tradableItem.RequiredBlockIndex > blockIndex)
                {
                    return;
                }
            }

            InventoryItemViewModel inventoryItem;
            switch (itemBase.ItemType)
            {
                case ItemType.Consumable:
                    inventoryItem = CreateInventoryItem(itemBase, count,
                        disabled: !Util.IsUsableItem(itemBase.Id));
                    _consumables.Add(inventoryItem);
                    break;
                case ItemType.Costume:
                    var costume = (Costume)itemBase;
                    inventoryItem = CreateInventoryItem(itemBase, count,
                        equipped: costume.equipped,
                        disabled: !Util.IsUsableItem(itemBase.Id));
                    _costumes.Add(inventoryItem);
                    break;
                case ItemType.Equipment:
                    var equipment = (Equipment)itemBase;
                    inventoryItem = CreateInventoryItem(itemBase, count,
                        equipped: equipment.equipped,
                        disabled: !Util.IsUsableItem(itemBase.Id));
                    _equipments.Add(inventoryItem);
                    break;
                case ItemType.Material:
                    var material = (Material)itemBase;
                    var istTradable = material is TradableMaterial;
                    if (TryGetMaterial(material, istTradable, out inventoryItem))
                    {
                        inventoryItem.Count.Value += count;
                    }

                    inventoryItem = CreateInventoryItem(itemBase, count);
                    _materials.Add(inventoryItem);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool TryGetMaterial(Material material, bool istTradable,
            out InventoryItemViewModel inventoryItem)
        {
            foreach (var item in _materials)
            {
                if (!(item.ItemBase is Material m) || !m.ItemId.Equals(material.ItemId))
                {
                    continue;
                }

                if (istTradable != item.ItemBase is TradableMaterial)
                {
                    continue;
                }

                inventoryItem = item;
                return true;
            }

            inventoryItem = null;
            return false;
        }

        private static InventoryItemViewModel CreateInventoryItem(ItemBase itemBase, int count,
            bool equipped = false, bool disabled = false)
        {
            return new InventoryItemViewModel(itemBase, count, equipped, disabled);
        }

        private void OnClickItem(InventoryItemViewModel item)
        {
            if (_selectedItem == null)
            {
                _selectedItem = item;
                _selectedItem.Selected.SetValueAndForceNotify(true);
                _onClickItem?.Invoke(_selectedItem, _selectedItem.View); // Show tooltip popup
            }
            else
            {
                if (_selectedItem.Equals(item))
                {
                    _selectedItem.Selected.SetValueAndForceNotify(false);
                    _selectedItem = null;
                }
                else
                {
                    _selectedItem.Selected.SetValueAndForceNotify(false);
                    _selectedItem = item;
                    _selectedItem.Selected.SetValueAndForceNotify(true);
                    _onClickItem?.Invoke(_selectedItem, _selectedItem.View); // Show tooltip popup
                }
            }
        }

        private ReactiveCollection<InventoryItemViewModel> GetModels(ItemType itemType)
        {
            return itemType switch
            {
                ItemType.Consumable => _consumables,
                ItemType.Costume => _costumes,
                ItemType.Equipment => _equipments,
                ItemType.Material => _materials,
                _ => throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null)
            };
        }

        private void OnDoubleClick(InventoryItemViewModel item)
        {
            _selectedItem?.Selected.SetValueAndForceNotify(false);
            _selectedItem = null;
            _onDoubleClickItem?.Invoke(item);
        }

        private void UpdateEquipmentNotification(List<ElementalType> elementalTypes = null)
        {
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            if (currentAvatarState is null)
            {
                return;
            }

            if (_activeItemType != ItemType.Equipment)
            {
                return;
            }

            var usableEquipments =
                _equipments.Where(x => Util.IsUsableItem(x.ItemBase.Id)).ToList();
            foreach (var item in usableEquipments)
            {
                item.HasNotification.Value = false;
            }

            var level = currentAvatarState.level;
            var availableSlots = UnlockHelper.GetAvailableEquipmentSlots(level);

            foreach (var (type, slotCount) in availableSlots)
            {
                var matchedEquipments = usableEquipments
                    .Where(e => e.ItemBase.ItemSubType == type);

                if (elementalTypes != null)
                {
                    matchedEquipments = matchedEquipments.Where(e =>
                        elementalTypes.Exists(x => x == e.ItemBase.ElementalType));
                }

                var equippedEquipments = matchedEquipments.Where(e => e.Equipped.Value);
                var unequippedEquipments = matchedEquipments.Where(e => !e.Equipped.Value)
                    .OrderByDescending(i => CPHelper.GetCP(i.ItemBase as Equipment));

                var equippedCount = equippedEquipments.Count();
                if (equippedCount < slotCount)
                {
                    var itemsToNotify = unequippedEquipments.Take(slotCount - equippedCount);
                    foreach (var item in itemsToNotify)
                    {
                        item.HasNotification.Value = true;
                    }
                }
                else
                {
                    var itemsToNotify =
                        unequippedEquipments.Where(e =>
                        {
                            var cp = CPHelper.GetCP(e.ItemBase as Equipment);
                            return equippedEquipments.Any(i =>
                                CPHelper.GetCP(i.ItemBase as Equipment) < cp);
                        }).Take(slotCount);
                    foreach (var item in itemsToNotify)
                    {
                        item.HasNotification.Value = true;
                    }
                }
            }
        }

        public void SetAction(Action<InventoryItemViewModel, RectTransform> clickItem,
            Action<InventoryItemViewModel> doubleClickItem,
            System.Action clickEquipmentToggle = null,
            System.Action clickCostumeToggle = null,
            System.Action onToggleConsumable = null)
        {
            _onClickItem = clickItem;
            _onDoubleClickItem = doubleClickItem;
            _onToggleEquipment = clickEquipmentToggle;
            _onToggleCostume = clickCostumeToggle;
            _onToggleConsumable = onToggleConsumable;
        }

        public void ClearSelectedItem()
        {
            _selectedItem?.Selected.SetValueAndForceNotify(false);
            _selectedItem = null;
            DisableFocus();
        }

        public void Focus(ItemType itemType, ItemSubType subType)
        {
            foreach (var model in GetModels(itemType))
            {
                if (model.ItemBase.ItemSubType.Equals(subType))
                {
                    model.Focused.Value = !model.Focused.Value;
                }
                else
                {
                    model.Focused.Value = false;
                }
            }
        }

        public void DisableFocus()
        {
            foreach (var model in _equipments)
            {
                model.Focused.Value = false;
            }

            foreach (var model in _costumes)
            {
                model.Focused.Value = false;
            }
        }

        public bool TryGetFirstCell(out InventoryItemViewModel cell)
        {
            return scroll.TryGetFirstItem(out cell);
        }

        public bool TryGetItemViewModel(ItemBase itemBase, out InventoryItemViewModel result)
        {
            result = null;
            var item = itemBase as INonFungibleItem;
            var models = GetModels(itemBase.ItemType);
            foreach (var model in models)
            {
                if (model.ItemBase is INonFungibleItem nonFungibleItem)
                {
                    if (nonFungibleItem.NonFungibleId.Equals(item.NonFungibleId))
                    {
                        result = model;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
