using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
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

        private readonly Dictionary<ItemSubType, List<InventoryItem>> _equipments =
            new Dictionary<ItemSubType, List<InventoryItem>>();

        private readonly List<InventoryItem> _consumables =
            new List<InventoryItem>();

        private readonly List<InventoryItem> _materials =
            new List<InventoryItem>();

        private readonly List<InventoryItem> _costumes =
            new List<InventoryItem>();

        private readonly ToggleGroup _toggleGroup = new ToggleGroup();

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private readonly List<InventoryItem> _cachedNotificationItems = new List<InventoryItem>();
        private readonly List<InventoryItem> _cachedFocusItems = new List<InventoryItem>();
        private readonly List<InventoryItem> _cachedBestItems = new List<InventoryItem>();

        private readonly Dictionary<ItemType, List<Predicate<InventoryItem>>> _dimConditionFuncsByItemType =
            new Dictionary<ItemType, List<Predicate<InventoryItem>>>();

        private static readonly ItemType[] ItemTypes =
            Enum.GetValues(typeof(ItemType)) as ItemType[];

        private InventoryItem _selectedModel;

        private Action<InventoryItem, RectTransform> _onClickItem;
        private Action<InventoryItem> _onDoubleClickItem;
        private System.Action _onToggleEquipment;
        private System.Action _onToggleCostume;
        private readonly List<ElementalType> _elementalTypes = new List<ElementalType>();
        private ItemType _activeItemType = ItemType.Equipment;
        private bool _checkTradable;
        private bool _reverseOrder;
        private bool _allowMoveTab;
        private string _notAllowedMoveTabMessage;

        public bool HasNotification => _equipments.Any(x => x.Value.Any(item=> item.HasNotification.Value));

        protected void Awake()
        {
            _toggleGroup.RegisterToggleable(equipmentButton);
            _toggleGroup.RegisterToggleable(consumableButton);
            _toggleGroup.RegisterToggleable(materialButton);
            _toggleGroup.RegisterToggleable(costumeButton);
            _toggleGroup.DisabledFunc = () => !_allowMoveTab;

            equipmentButton.OnClick.Subscribe(button =>
                {
                    OnTabButtonClick(button, ItemType.Equipment, _onToggleEquipment);
                })
                .AddTo(gameObject);
            costumeButton.OnClick.Subscribe(button =>
                {
                    OnTabButtonClick(button, ItemType.Costume, _onToggleCostume);
                })
                .AddTo(gameObject);
            consumableButton.OnClick
                .Subscribe(button =>
                {
                    OnTabButtonClick(button, ItemType.Consumable);
                })
                .AddTo(gameObject);
            materialButton.OnClick.Subscribe(button =>
                {
                    OnTabButtonClick(button, ItemType.Material);
                })
                .AddTo(gameObject);

            foreach (var type in ItemTypes)
            {
                _dimConditionFuncsByItemType[type] = new List<Predicate<InventoryItem>>();
            }
        }

        private void OnTabButtonClick(IToggleable toggleable, ItemType type, System.Action onSetToggle = null)
        {
            if (_allowMoveTab)
            {
                SetToggle(toggleable, type);
                onSetToggle?.Invoke();
            }
            else
            {
                OneLineSystem.Push(MailType.System, _notAllowedMoveTabMessage, NotificationCell.NotificationType.Notification);
            }
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

        private void Set(Action<Inventory, Nekoyume.Model.Item.Inventory> onUpdateInventory = null,
            List<(ItemType type, Predicate<InventoryItem> predicate)> itemSetDimPredicates = null)
        {
            _disposables.DisposeAllAndClear();
            foreach (var type in ItemTypes)
            {
                _dimConditionFuncsByItemType[type].Clear();
            }

            itemSetDimPredicates?.ForEach(tuple =>
                _dimConditionFuncsByItemType[tuple.type].Add(tuple.predicate));

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
                foreach (var item in
                         inventory.Items.OrderByDescending(x => x.item is ITradableItem))
                {
                    if (item.Locked)
                    {
                        continue;
                    }

                    AddItem(item.item, item.count);
                }

                var models = GetModels(_activeItemType);
                if (_reverseOrder)
                {
                    models.Reverse();
                }

                scroll.UpdateData(models, resetScrollOnEnable);
                UpdateDimmedInventoryItem();

                onUpdateInventory?.Invoke(this, inventory);
            }).AddTo(_disposables);

            SetToggle(equipmentButton, ItemType.Equipment);
            scroll.OnClick.Subscribe(OnClickItem).AddTo(_disposables);
            scroll.OnDoubleClick.Subscribe(OnDoubleClick).AddTo(_disposables);
        }

        private void SetToggle(IToggleable toggle, ItemType itemType)
        {
            _activeItemType = itemType;
            scroll.UpdateData(GetModels(itemType), !toggle.IsToggledOn);
            UpdateDimmedInventoryItem();

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
                        levelLimited: !Util.IsUsableItem(itemBase));
                    _consumables.Add(inventoryItem);
                    break;
                case ItemType.Costume:
                    var costume = (Costume)itemBase;
                    inventoryItem = CreateInventoryItem(itemBase, count,
                        equipped: costume.equipped,
                        levelLimited: !Util.IsUsableItem(itemBase));
                    _costumes.Add(inventoryItem);
                    break;
                case ItemType.Equipment:
                    var equipment = (Equipment)itemBase;
                    inventoryItem = CreateInventoryItem(itemBase, count,
                        equipped: equipment.equipped,
                        levelLimited: !Util.IsUsableItem(itemBase));

                    if (!_equipments.ContainsKey(itemBase.ItemSubType))
                    {
                        _equipments.Add(itemBase.ItemSubType, new List<InventoryItem>());
                    }

                    _equipments[itemBase.ItemSubType].Add(inventoryItem);
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

        private List<InventoryItem> GetModels(ItemType itemType)
        {
            return itemType switch
            {
                ItemType.Consumable => _consumables,
                ItemType.Costume => _costumes,
                ItemType.Equipment => GetOrganizedEquipments(),
                ItemType.Material => GetOrganizedMaterials(),
                _ => throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null)
            };
        }

        private void OnDoubleClick(InventoryItem item)
        {
            _selectedModel?.Selected.SetValueAndForceNotify(false);
            _selectedModel = null;
            _onDoubleClickItem?.Invoke(item);
        }

        private List<InventoryItem> GetOrganizedEquipments()
        {
            var bestItems = GetUsableBestEquipments();
            UpdateEquipmentNotification(bestItems);
            var result = new List<InventoryItem>();
            foreach (var pair in _equipments)
            {
                result.AddRange(pair.Value);
            }

            result = result.OrderByDescending(x => bestItems.Exists(y => y.Equals(x)))
                .ThenBy(x => x.ItemBase.ItemSubType)
                .ThenByDescending(x => Util.IsUsableItem(x.ItemBase)).ToList();

            if (_elementalTypes.Any())
            {
                result = result.OrderByDescending(x =>
                    _elementalTypes.Exists(y => y.Equals(x.ItemBase.ElementalType))).ToList();
            }

            return result;
        }

        private List<InventoryItem> GetOrganizedMaterials()
        {
            return _materials.OrderByDescending(x =>
                    x.ItemBase.ItemSubType == ItemSubType.ApStone ||
                    x.ItemBase.ItemSubType == ItemSubType.Hourglass)
                .ThenBy(x => x.ItemBase is ITradableItem).ToList();
        }

        private void UpdateEquipmentNotification(IEnumerable<InventoryItem> bestItems)
        {
            if (_activeItemType != ItemType.Equipment)
            {
                return;
            }

            foreach (var item in _cachedNotificationItems)
            {
                item.HasNotification.Value = false;
            }

            _cachedNotificationItems.Clear();

            foreach (var item in bestItems.Where(item => !item.Equipped.Value))
            {
                item.HasNotification.Value = true;
                _cachedNotificationItems.Add(item);
            }
        }

        private List<InventoryItem> GetUsableBestEquipments()
        {
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            if (currentAvatarState is null)
            {
                return new List<InventoryItem>();
            }

            var level = currentAvatarState.level;
            var availableSlots = UnlockHelper.GetAvailableEquipmentSlots(level);

            var bestItems = new List<InventoryItem>();
            var selectedEquipments = new Dictionary<ItemSubType, List<InventoryItem>>();
            if (_elementalTypes.Any())
            {
                foreach (var pair in _equipments)
                {
                    foreach (var item in pair.Value)
                    {
                        if (!_elementalTypes.Exists(x => x.Equals(item.ItemBase.ElementalType)))
                        {
                            continue;
                        }

                        if (!selectedEquipments.ContainsKey(item.ItemBase.ItemSubType))
                        {
                            selectedEquipments.Add(item.ItemBase.ItemSubType,
                                new List<InventoryItem>());
                        }

                        selectedEquipments[item.ItemBase.ItemSubType].Add(item);
                    }
                }
            }
            else
            {
                selectedEquipments = _equipments;
            }

            foreach (var pair in selectedEquipments)
            {
                var (_, slotCount) = availableSlots.FirstOrDefault(x => x.Item1.Equals(pair.Key));
                var item = pair.Value.Where(x => Util.IsUsableItem(x.ItemBase))
                    .OrderByDescending(x => CPHelper.GetCP(x.ItemBase as Equipment))
                    .Take(slotCount);
                bestItems.AddRange(item);
            }

            return bestItems;
        }

        private void UpdateDimmedInventoryItem()
        {
            foreach (var itemType in ItemTypes)
            {
                if (_dimConditionFuncsByItemType[itemType].Any())
                {
                    foreach (var inventoryItem in itemType switch
                             {
                                 ItemType.Consumable => _consumables,
                                 ItemType.Costume => _costumes,
                                 ItemType.Equipment => _equipments.SelectMany(pair => pair.Value),
                                 ItemType.Material => _materials,
                                 _ => throw new ArgumentOutOfRangeException()
                             })
                    {
                        inventoryItem.DimObjectEnabled.Value =
                            _dimConditionFuncsByItemType[itemType]
                                .Any(predicate => predicate.Invoke(inventoryItem));
                    }
                }
            }
        }

        public void SetAvatarInfo(Action<InventoryItem, RectTransform> clickItem,
            Action<InventoryItem> doubleClickItem,
            System.Action clickEquipmentToggle,
            System.Action clickCostumeToggle,
            IEnumerable<ElementalType> elementalTypes)
        {
            _reverseOrder = false;
            SetAction(clickItem, doubleClickItem, clickEquipmentToggle, clickCostumeToggle);
            var predicateByElementalType = InventoryHelper.GetDimmedFuncByElementalTypes(elementalTypes.ToList());
            var predicateList = predicateByElementalType != null
                ? new List<(ItemType type, Predicate<InventoryItem>)>
                    {(ItemType.Equipment, predicateByElementalType)}
                : null;
            Set(itemSetDimPredicates: predicateList);
            _allowMoveTab = true;
        }

        public void SetShop(Action<InventoryItem, RectTransform> clickItem)
        {
            _reverseOrder = false;
            _checkTradable = true;
            SetAction(clickItem);
            Set();
            _allowMoveTab = true;
        }

        public void SetGrinding(Action<InventoryItem, RectTransform> clickItem,
            Action<Inventory, Nekoyume.Model.Item.Inventory> onUpdateInventory,
            List<(ItemType type, Predicate<InventoryItem>)> predicateList,
            bool reverseOrder)
        {
            _reverseOrder = reverseOrder;
            SetAction(clickItem);
            Set(onUpdateInventory, predicateList);
            _allowMoveTab = false;
            _notAllowedMoveTabMessage = L10nManager.Localize("ERROR_NOT_GRINDING_TABCHANGE");
        }

        public void ClearSelectedItem()
        {
            _selectedModel?.Selected.SetValueAndForceNotify(false);
            _selectedModel = null;
            ClearFocus();
        }

        public void Focus(ItemType itemType, ItemSubType subType,
            List<ElementalType> elementalTypes)
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
                            if (model.Focused.Value)
                            {
                                _cachedFocusItems.Add(model);
                            }
                        }
                        else
                        {
                            model.Focused.Value = false;
                        }
                    }
                    else
                    {
                        model.Focused.Value = !model.Focused.Value;
                        if (model.Focused.Value)
                        {
                            _cachedFocusItems.Add(model);
                        }
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
            foreach (var item in _cachedFocusItems)
            {
                item.Focused.Value = false;
            }

            _cachedFocusItems.Clear();
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
