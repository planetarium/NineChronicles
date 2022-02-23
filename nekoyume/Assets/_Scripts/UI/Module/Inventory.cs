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

        [SerializeField]
        private bool resetScrollOnEnable;

        private readonly ReactiveCollection<InventoryItem> _equipments =
            new ReactiveCollection<InventoryItem>();

        private readonly ReactiveCollection<InventoryItem> _consumables =
            new ReactiveCollection<InventoryItem>();

        private readonly ReactiveCollection<InventoryItem> _materials =
            new ReactiveCollection<InventoryItem>();

        private readonly ReactiveCollection<InventoryItem> _costumes =
            new ReactiveCollection<InventoryItem>();

        private readonly ToggleGroup _toggleGroup = new ToggleGroup();

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private InventoryItem _selectedModel;

        private Action<InventoryItem, RectTransform> _onClickItem;
        private Action<InventoryItem> _onDoubleClickItem;
        private System.Action _onToggleEquipment;
        private System.Action _onToggleCostume;
        private readonly List<ElementalType> _elementalTypes = new List<ElementalType>();
        private ItemType _activeItemType = ItemType.Equipment;
        private bool _checkTradable;

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
                .Subscribe(_ => { SetToggle(consumableButton, ItemType.Consumable); })
                .AddTo(gameObject);
            materialButton.OnClick.Subscribe(_ => SetToggle(materialButton, ItemType.Material))
                .AddTo(gameObject);
        }

        private void SetAction(Action<InventoryItem, RectTransform> clickItem,
            Action<InventoryItem> doubleClickItem = null,
            System.Action clickEquipmentToggle = null,
            System.Action clickCostumeToggle = null)
        {
            _onClickItem = clickItem;
            _onDoubleClickItem = doubleClickItem;
            _onToggleEquipment = clickEquipmentToggle;
            _onToggleCostume = clickCostumeToggle;
        }

        private void SetElementalTypes(IEnumerable<ElementalType> elementalTypes)
        {
            _elementalTypes.Clear();
            _elementalTypes.AddRange(elementalTypes);
        }

        private void Set()
        {
            _disposables.DisposeAllAndClear();
            ReactiveAvatarState.Inventory.Subscribe(inventory =>
            {
                _equipments.Clear();
                _consumables.Clear();
                _materials.Clear();
                _costumes.Clear();

                if (inventory is null)
                {
                    return;
                }

                _selectedModel = null;
                foreach (var item in inventory.Items.OrderByDescending(x => x.item is ITradableItem))
                {
                    if (item.Locked)
                    {
                        continue;
                    }

                    AddItem(item.item, item.count);
                }

                scroll.UpdateData(GetModels(_activeItemType), resetScrollOnEnable);
                UpdateEquipmentNotification(_elementalTypes);
                UpdateElementalTypeDisable(_elementalTypes);
            }).AddTo(_disposables);

            SetToggle(equipmentButton, ItemType.Equipment);
            scroll.OnClick.Subscribe(OnClickItem).AddTo(_disposables);
            scroll.OnDoubleClick.Subscribe(OnDoubleClick).AddTo(_disposables);
        }

        private void SetToggle(IToggleable toggle, ItemType itemType)
        {
            _activeItemType = itemType;
            scroll.UpdateData(GetModels(itemType), !toggle.IsToggledOn);
            UpdateEquipmentNotification(_elementalTypes);
            UpdateElementalTypeDisable(_elementalTypes);
            ClearFocus();
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

            InventoryItem inventoryItem;
            switch (itemBase.ItemType)
            {
                case ItemType.Consumable:
                    inventoryItem = CreateInventoryItem(itemBase, count,
                        levelLimited: !Util.IsUsableItem(itemBase.Id));
                    _consumables.Add(inventoryItem);
                    break;
                case ItemType.Costume:
                    var costume = (Costume)itemBase;
                    inventoryItem = CreateInventoryItem(itemBase, count,
                        equipped: costume.equipped,
                        levelLimited: !Util.IsUsableItem(itemBase.Id));
                    _costumes.Add(inventoryItem);
                    break;
                case ItemType.Equipment:
                    var equipment = (Equipment)itemBase;
                    inventoryItem = CreateInventoryItem(itemBase, count,
                        equipped: equipment.equipped,
                        levelLimited: !Util.IsUsableItem(itemBase.Id));
                    _equipments.Add(inventoryItem);
                    break;
                case ItemType.Material:
                    var material = (Material)itemBase;
                    var istTradable = material is TradableMaterial;
                    if (TryGetMaterial(material, istTradable, out inventoryItem))
                    {
                        inventoryItem.Count.Value += count;
                    }
                    else
                    {
                        inventoryItem = CreateInventoryItem(itemBase, count);
                        _materials.Add(inventoryItem);
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool TryGetMaterial(Material material, bool isTradable, out InventoryItem model)
        {
            foreach (var item in _materials)
            {
                if (!(item.ItemBase is Material m) || !m.ItemId.Equals(material.ItemId))
                {
                    continue;
                }

                if (isTradable != item.ItemBase is TradableMaterial)
                {
                    continue;
                }

                model = item;
                return true;
            }

            model = null;
            return false;
        }

        private InventoryItem CreateInventoryItem(ItemBase itemBase, int count,
            bool equipped = false, bool levelLimited = false)
        {
            return new InventoryItem(itemBase, count, equipped, levelLimited,
                _checkTradable && !(itemBase is ITradableItem));
        }

        private void OnClickItem(InventoryItem item)
        {
            if (_selectedModel == null)
            {
                _selectedModel = item;
                _selectedModel.Selected.SetValueAndForceNotify(true);
                _onClickItem?.Invoke(_selectedModel, _selectedModel.View); // Show tooltip popup
            }
            else
            {
                if (_selectedModel.Equals(item))
                {
                    _selectedModel.Selected.SetValueAndForceNotify(false);
                    _selectedModel = null;
                }
                else
                {
                    _selectedModel.Selected.SetValueAndForceNotify(false);
                    _selectedModel = item;
                    _selectedModel.Selected.SetValueAndForceNotify(true);
                    _onClickItem?.Invoke(_selectedModel, _selectedModel.View); // Show tooltip popup
                }
            }
        }

        private ReactiveCollection<InventoryItem> GetModels(ItemType itemType)
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

        private void OnDoubleClick(InventoryItem item)
        {
            _selectedModel?.Selected.SetValueAndForceNotify(false);
            _selectedModel = null;
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

        private void UpdateElementalTypeDisable(List<ElementalType> elementalTypes)
        {
            if (elementalTypes == null || !elementalTypes.Any())
            {
                return;
            }

            foreach (var model in _equipments)
            {
                var elementalType = model.ItemBase.ElementalType;
                model.ElementalTypeDisabled.Value = !elementalTypes.Exists(x => x.Equals(elementalType));
            }
        }

        public void SetAvatarInfo(Action<InventoryItem, RectTransform> clickItem,
            Action<InventoryItem> doubleClickItem,
            System.Action clickEquipmentToggle,
            System.Action clickCostumeToggle,
            IEnumerable<ElementalType> elementalTypes)
        {
            SetAction(clickItem, doubleClickItem, clickEquipmentToggle, clickCostumeToggle);
            SetElementalTypes(elementalTypes);
            Set();
        }

        public void SetShop(Action<InventoryItem, RectTransform> clickItem)
        {
            _checkTradable = true;
            SetAction(clickItem);
            Set();
        }

        public void ClearSelectedItem()
        {
            _selectedModel?.Selected.SetValueAndForceNotify(false);
            _selectedModel = null;
            ClearFocus();
        }

        public void Focus(ItemType itemType, ItemSubType subType, List<ElementalType> elementalTypes)
        {
            foreach (var model in GetModels(itemType))
            {
                if (model.ItemBase.ItemSubType.Equals(subType))
                {
                    if (model.ItemBase.ItemType == ItemType.Equipment)
                    {
                        if (elementalTypes.Exists(x => x.Equals(model.ItemBase.ElementalType)))
                        {
                            model.Focused.Value = !model.Focused.Value;
                        }
                        else
                        {
                            model.Focused.Value = false;
                        }
                    }
                    else
                    {
                        model.Focused.Value = !model.Focused.Value;
                    }
                }
                else
                {
                    model.Focused.Value = false;
                }
            }
        }

        public void ClearFocus()
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

        public bool TryGetModel(ItemBase itemBase, out InventoryItem result)
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

        #region For tutorial

        public bool TryGetFirstCell(out InventoryItem cell)
        {
            return scroll.TryGetFirstItem(out cell);
        }

        #endregion
    }
}
