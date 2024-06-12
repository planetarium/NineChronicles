using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Rune;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UnityEngine;
using Material = Nekoyume.Model.Item.Material;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class Inventory : MonoBehaviour
    {
        public enum InventoryTabType
        {
            Equipment,
            Consumable,
            Rune,
            Material,
            Costume,
            FungibleAsset,
        }

        [SerializeField]
        private CategoryTabButton equipmentButton = null;

        [SerializeField]
        private CategoryTabButton consumableButton = null;

        [SerializeField]
        private CategoryTabButton runeButton = null;

        [SerializeField]
        private CategoryTabButton materialButton = null;

        [SerializeField]
        private CategoryTabButton costumeButton = null;

        [SerializeField]
        private CategoryTabButton fungibleAssetButton = null;

        [SerializeField]
        private InventoryScroll scroll = null;

        [SerializeField]
        private bool resetScrollOnEnable;

        [SerializeField]
        private RectTransform tooltipSocket;

        private readonly Dictionary<ItemSubType, List<InventoryItem>> _equipments = new();
        private readonly List<InventoryItem> _consumables = new();
        private readonly List<InventoryItem> _materials = new();
        private readonly List<InventoryItem> _costumes = new();
        private readonly List<InventoryItem> _runes = new();
        private readonly List<InventoryItem> _fungibleAssets = new();

        private readonly ToggleGroup _toggleGroup = new();

        private readonly List<IDisposable> _disposablesOnSet = new();
        private readonly List<InventoryItem> _cachedNotificationEquipments = new();
        private readonly List<InventoryItem> _cachedNotificationCostumes = new();
        private readonly List<InventoryItem> _cachedNotificationRunes = new();
        private readonly List<InventoryItem> _cachedFocusItems = new();
        private readonly List<ElementalType> _elementalTypes = new();

        private readonly Dictionary<ItemType, List<Predicate<InventoryItem>>> _dimConditionFuncsByItemType = new();
        private static readonly ItemType[] ItemTypes = Enum.GetValues(typeof(ItemType)) as ItemType[];

        private InventoryItem _selectedModel;

        private Action<InventoryItem> _onClickItem;
        private Action<InventoryItem> _onDoubleClickItem;
        private Action<InventoryTabType> _onClickTab;
        private InventoryTabType _activeTabType = InventoryTabType.Equipment;
        private bool _checkTradable;

        protected void Awake()
        {
            _toggleGroup.RegisterToggleable(equipmentButton);
            _toggleGroup.RegisterToggleable(consumableButton);
            _toggleGroup.RegisterToggleable(runeButton);
            _toggleGroup.RegisterToggleable(materialButton);
            _toggleGroup.RegisterToggleable(costumeButton);
            _toggleGroup.RegisterToggleable(fungibleAssetButton);

            equipmentButton.OnClick
                .Subscribe(button => OnTabButtonClick(button, InventoryTabType.Equipment))
                .AddTo(gameObject);
            costumeButton.OnClick
                .Subscribe(button => OnTabButtonClick(button, InventoryTabType.Costume))
                .AddTo(gameObject);
            runeButton.OnClick
                .Subscribe(button => OnTabButtonClick(button, InventoryTabType.Rune))
                .AddTo(gameObject);
            consumableButton.OnClick
                .Subscribe(button => OnTabButtonClick(button, InventoryTabType.Consumable))
                .AddTo(gameObject);
            materialButton.OnClick
                .Subscribe(button => OnTabButtonClick(button, InventoryTabType.Material))
                .AddTo(gameObject);
            fungibleAssetButton.OnClick
                .Subscribe(button => OnTabButtonClick(button, InventoryTabType.FungibleAsset))
                .AddTo(gameObject);

            foreach (var type in ItemTypes)
            {
                _dimConditionFuncsByItemType[type] = new List<Predicate<InventoryItem>>();
            }
        }

        private void OnTabButtonClick(IToggleable toggleable, InventoryTabType tabType)
        {
            if (!_toggleGroup.DisabledFunc.Invoke())
            {
                SetToggle(toggleable, tabType);
                _onClickTab?.Invoke(tabType);
            }
            else
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("ERROR_NOT_GRINDING_TABCHANGE"),
                    NotificationCell.NotificationType.Notification);
            }
        }

        private void SetAction(Action<InventoryItem> clickItem,
            Action<InventoryItem> doubleClickItem = null,
            Action<InventoryTabType> onClickTab = null)
        {
            _onClickItem = clickItem;
            _onDoubleClickItem = doubleClickItem;
            _onClickTab = onClickTab;
        }

        private void SetInventoryTab(
            List<(ItemType type, Predicate<InventoryItem> predicate)> itemSetDimPredicates = null,
            Action<Inventory, Nekoyume.Model.Item.Inventory> onUpdateInventory = null,
            BattleType battleType = BattleType.Adventure,
            bool useConsumable = false,
            bool reverseOrder = false)
        {
            _disposablesOnSet.DisposeAllAndClear();
            foreach (var type in ItemTypes)
            {
                _dimConditionFuncsByItemType[type]?.Clear();
            }

            itemSetDimPredicates?.ForEach(tuple =>
                _dimConditionFuncsByItemType[tuple.type]?.Add(tuple.predicate));

            ReactiveAvatarState.Inventory
                .Subscribe(e =>
                {
                    SetInventory(e, onUpdateInventory, battleType, reverseOrder);
                })
                .AddTo(_disposablesOnSet);

            if (useConsumable && _consumables.Any())
            {
                SetToggle(consumableButton, InventoryTabType.Consumable);
            }
            else
            {
                SetToggle(equipmentButton, InventoryTabType.Equipment);
            }

            scroll.OnClick.Subscribe(OnClickItem).AddTo(_disposablesOnSet);
            scroll.OnDoubleClick.Subscribe(OnDoubleClick).AddTo(_disposablesOnSet);
        }

        private void SetToggle(IToggleable toggle, InventoryTabType tabType)
        {
            _activeTabType = tabType;
            scroll.UpdateData(GetModels(tabType), !toggle.IsToggledOn);
            UpdateDimmedInventoryItem();

            ClearFocus();
            _toggleGroup.SetToggledOffAll();
            toggle.SetToggledOn();
            AudioController.PlayClick();
        }

        private void SetInventory(
            Nekoyume.Model.Item.Inventory inventory,
            Action<Inventory, Nekoyume.Model.Item.Inventory> onUpdateInventory = null,
            BattleType battleType = BattleType.Adventure,
            bool reverseOrder = false)
        {
            _equipments.Clear();
            _consumables.Clear();
            _materials.Clear();
            _costumes.Clear();
            _runes.Clear();
            _fungibleAssets.Clear();

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

            foreach (var runeState in States.Instance.AllRuneState.Runes.Values)
            {
                _runes.Add(new InventoryItem(runeState));
            }

            foreach (var fav in States.Instance.CurrentAvatarBalances.Values)
            {
                _fungibleAssets.Add(new InventoryItem(fav));
            }

            var models = GetModels(_activeTabType);
            if (reverseOrder)
            {
                models.Reverse();
            }

            scroll.UpdateData(models, resetScrollOnEnable);
            UpdateDimmedInventoryItem();
            onUpdateInventory?.Invoke(this, inventory);

            var itemSlotState = States.Instance.CurrentItemSlotStates[battleType];
            UpdateEquipmentEquipped(itemSlotState.Equipments);
            UpdateCostumes(itemSlotState.Costumes);
            var equippedRuneState = States.Instance.GetEquippedRuneStates(battleType);
            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            UpdateRuneEquipped(equippedRuneState, battleType, runeListSheet);
            UpdateRuneNotification(GetBestRunes(battleType));
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
                    if (TryGetConsumable(itemBase.Id, out inventoryItem))
                    {
                        inventoryItem.Count.Value += count;
                    }
                    else
                    {
                        inventoryItem = CreateInventoryItem(
                            itemBase,
                            count,
                            levelLimited: !Util.IsUsableItem(itemBase));
                        _consumables.Add(inventoryItem);
                    }

                    break;
                case ItemType.Costume:
                    inventoryItem = CreateInventoryItem(
                        itemBase,
                        count,
                        levelLimited: !Util.IsUsableItem(itemBase));
                    _costumes.Add(inventoryItem);
                    break;
                case ItemType.Equipment:
                    inventoryItem = CreateInventoryItem(
                        itemBase,
                        count,
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
            model = _materials.FirstOrDefault(item =>
                item.ItemBase is Material m && m.ItemId.Equals(material.ItemId) &&
                isTradable == item.ItemBase is TradableMaterial);

            return model != null;
        }

        public bool TryGetConsumable(int itemId, out InventoryItem model)
        {
            model = _consumables.FirstOrDefault(item => item.ItemBase.Id.Equals(itemId));

            return model != null;
        }

        private InventoryItem CreateInventoryItem(
            ItemBase itemBase,
            int count,
            bool levelLimited = false)
        {
            return new InventoryItem(
                itemBase,
                count,
                levelLimited,
                _checkTradable && !(itemBase is ITradableItem));
        }

        private void OnClickItem(InventoryItem item)
        {
            if (_selectedModel == null)
            {
                _selectedModel = item;
                _selectedModel.Selected.SetValueAndForceNotify(true);
                _onClickItem?.Invoke(_selectedModel); // Show tooltip popup
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
                    _onClickItem?.Invoke(_selectedModel); // Show tooltip popup
                }
            }
        }

        private List<InventoryItem> GetModels(InventoryTabType tabType)
        {
            return tabType switch
            {
                InventoryTabType.Consumable => _consumables,
                InventoryTabType.Costume => _costumes,
                InventoryTabType.Equipment => GetOrganizedEquipments(),
                InventoryTabType.Material => GetOrganizedMaterials(),
                InventoryTabType.Rune => GetOrganizedRunes(),
                InventoryTabType.FungibleAsset => GetOrganizedFungibleAssets(),
                _ => throw new ArgumentOutOfRangeException(nameof(tabType), tabType, null)
            };
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
            if (item.ItemBase is null && item.RuneState is null)
            {
                return;
            }

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

            result = result
                .OrderByDescending(x => x.Equipped.Value)
                .ThenByDescending(x => bestItems.Exists(y => y.Equals(x)))
                .ThenBy(x => {
                    if (x.ItemBase.ItemSubType == ItemSubType.Aura)
                        return 0;
                    return (int)x.ItemBase.ItemSubType;
                })
                .ThenByDescending(x => Util.IsUsableItem(x.ItemBase))
                .ThenByDescending(x => CPHelper.GetCP(x.ItemBase as Equipment))
                .ToList();

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

        private List<InventoryItem> GetOrganizedRunes()
        {
            return _runes.OrderBy(x => x.DimObjectEnabled.Value).ToList();
        }

        private List<InventoryItem> GetOrganizedFungibleAssets()
        {
            return _fungibleAssets;
        }

        private void UpdateEquipmentNotification(IEnumerable<InventoryItem> bestItems)
        {
            if (_activeTabType != InventoryTabType.Equipment)
            {
                return;
            }

            foreach (var item in _cachedNotificationEquipments)
            {
                item.HasNotification.Value = false;
            }

            _cachedNotificationEquipments.Clear();

            foreach (var item in bestItems.Where(item => !item.Equipped.Value))
            {
                item.HasNotification.Value = true;
                _cachedNotificationEquipments.Add(item);
            }

            equipmentButton.HasNotification.SetValueAndForceNotify(_cachedNotificationEquipments.Any());
        }

        private void UpdateCostumeNotification(List<Guid> costumes, IEnumerable<InventoryItem> bestItems)
        {
            foreach (var item in _cachedNotificationCostumes)
            {
                item.HasNotification.Value = false;
            }

            _cachedNotificationCostumes.Clear();

            var equippedCostumes = costumes
                .Select(costume => _costumes.FirstOrDefault(x => ((Costume)x.ItemBase).ItemId == costume))
                .Where(x => x is not null)
                .ToList();
            foreach (var item in bestItems.Where(item => !item.Equipped.Value))
            {
                var equippedItem = equippedCostumes.FirstOrDefault(
                    x => x.ItemBase.ItemSubType == item.ItemBase.ItemSubType);
                if (equippedItem != null)
                {
                    var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
                    var cp = CPHelper.GetCP(item.ItemBase as Costume, costumeSheet);
                    var equippedCostumeCp = CPHelper.GetCP(equippedItem.ItemBase as Costume, costumeSheet);
                    item.HasNotification.Value = cp > equippedCostumeCp;
                }
                else
                {
                    item.HasNotification.Value = true;
                }
                _cachedNotificationCostumes.Add(item);
            }

            costumeButton.HasNotification.SetValueAndForceNotify(_cachedNotificationCostumes.Any());
        }

        private void UpdateRuneNotification(IEnumerable<InventoryItem> bestItems)
        {
            foreach (var item in _cachedNotificationRunes)
            {
                item.HasNotification.Value = false;
            }

            _cachedNotificationRunes.Clear();

            foreach (var item in bestItems.Where(item => !item.Equipped.Value))
            {
                item.HasNotification.Value = true;
                _cachedNotificationRunes.Add(item);
            }

            runeButton.HasNotification.SetValueAndForceNotify(_cachedNotificationRunes.Any());
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
                    .ThenByDescending(x => x.Equipped.Value ? 1 : 0)
                    .Take(slotCount);
                bestItems.AddRange(item);
            }

            return bestItems;
        }

        private List<InventoryItem> GetUsableBestCostumes()
        {
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            if (currentAvatarState is null)
            {
                return new List<InventoryItem>();
            }

            var sortedCostumes = new Dictionary<ItemSubType, List<InventoryItem>>()
            {
                { ItemSubType.FullCostume, new List<InventoryItem>()},
                { ItemSubType.Title, new List<InventoryItem>()},
            };
            foreach (var inventoryItem in _costumes.Where(x => sortedCostumes.ContainsKey(x.ItemBase.ItemSubType)))
            {
                sortedCostumes[inventoryItem.ItemBase.ItemSubType].Add(inventoryItem);
            }

            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var best = new List<InventoryItem>();
            foreach (var pair in sortedCostumes)
            {
                var highScore = pair.Value.Where(x => Util.IsUsableItem(x.ItemBase))
                    .Select(inventoryItem => CPHelper.GetCP(inventoryItem.ItemBase as Costume, costumeSheet))
                    .Prepend(0)
                    .Max();
                var item = pair.Value.Where(x => Util.IsUsableItem(x.ItemBase))
                    .Where(x => CPHelper.GetCP(x.ItemBase as Costume, costumeSheet) == highScore);
                best.AddRange(item);
            }

            return best;
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

        public bool HasNotification()
        {
            var clearedStageId = States.Instance.CurrentAvatarState
                .worldInformation.TryGetLastClearedStageId(out var id) ? id : 1;
            var equipments = GetBestEquipments();
            foreach (var guid in equipments)
            {
                var slots = States.Instance.CurrentItemSlotStates.Values;
                foreach (var slotState in slots.Where(x => !x.Equipments.Exists(x => x == guid)))
                {
                    if (slotState.BattleType == BattleType.Arena)
                    {
                        if (clearedStageId >= Game.LiveAsset.GameConfig.RequiredStage.Arena)
                        {
                            return true;
                        }
                    }
                    else if (slotState.BattleType == BattleType.Raid)
                    {
                        if (clearedStageId >= Game.LiveAsset.GameConfig.RequiredStage.Arena)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            for (var i = 1; i < (int)BattleType.End; i++)
            {
                var battleType = (BattleType)i;
                var inventoryItems = GetBestRunes(battleType);
                foreach (var inventoryItem in inventoryItems)
                {
                    var slots = States.Instance.CurrentRuneSlotStates[battleType].GetRuneSlot();
                    if (!slots.Exists(x => x.RuneId == inventoryItem.RuneState.RuneId))
                    {
                        if (battleType == BattleType.Arena)
                        {
                            if (clearedStageId >= Game.LiveAsset.GameConfig.RequiredStage.Arena)
                            {
                                return true;
                            }
                        }
                        else if (battleType == BattleType.Raid)
                        {
                            if (clearedStageId >= Game.LiveAsset.GameConfig.RequiredStage.Arena)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public InventoryItem GetBestEquipmentInventoryItem()
        {
            var bestItem = GetUsableBestEquipments().FirstOrDefault();
            OnClickItem(bestItem);
            return bestItem;
        }

        public List<Guid> GetBestEquipments()
        {
            return GetUsableBestEquipments()
                .Select(x => x.ItemBase as Equipment)
                .Select(x => x.ItemId)
                .ToList();
        }

        public List<InventoryItem> GetBestRunes(BattleType battleType)
        {
            var runes = new Dictionary<RuneType, List<InventoryItem>>()
            {
                { RuneType.Stat, new List<InventoryItem>() },
                { RuneType.Skill, new List<InventoryItem>() },
            };

            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            foreach (var item in _runes)
            {
                if (!runeListSheet.TryGetValue(item.RuneState.RuneId, out var rune))
                {
                    continue;
                }

                if (!battleType.IsEquippableRune((RuneUsePlace)rune.UsePlace))
                {
                    continue;
                }

                runes[(RuneType)rune.RuneType].Add(item);
            }

            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            var bestRunes = new List<InventoryItem>();
            foreach (var rune in runes.Values)
            {
                var best = rune.OrderByDescending(x =>
                    {
                        if (!runeOptionSheet.TryGetValue(x.RuneState.RuneId, out var optionRow))
                        {
                            return 0;
                        }

                        if (!optionRow.LevelOptionMap.TryGetValue(x.RuneState.Level, out var option))
                        {
                            return 0;
                        }

                        return option.Cp;
                    })
                    .Take(1);

                bestRunes.AddRange(best);
            }

            return bestRunes;
        }

        public void SetAvatarInformation(
            Action<InventoryItem> clickItem,
            Action<InventoryItem> doubleClickItem,
            Action<InventoryTabType> onClickTab,
            IEnumerable<ElementalType> elementalTypes,
            BattleType battleType,
            Action<Inventory, Nekoyume.Model.Item.Inventory> onUpdateInventory = null,
            bool useConsumable = false)
        {
            _elementalTypes.Clear();
            _elementalTypes.AddRange(elementalTypes);
            SetAction(clickItem, doubleClickItem, onClickTab);
            var predicateByElementalType =
                InventoryHelper.GetDimmedFuncByElementalTypes(elementalTypes.ToList());
            var predicateList = predicateByElementalType != null
                ? new List<(ItemType type, Predicate<InventoryItem>)>
                    { (ItemType.Equipment, predicateByElementalType) }
                : null;
            SetInventoryTab(predicateList, onUpdateInventory:onUpdateInventory, battleType, useConsumable:useConsumable);
            _toggleGroup.DisabledFunc = () => false;
        }

        public void SetShop(Action<InventoryItem> clickItem)
        {
            _checkTradable = true;
            SetAction(clickItem);
            SetInventoryTab();
            _toggleGroup.DisabledFunc = () => false;
        }

        public void SetGrinding(Action<InventoryItem> clickItem,
            Action<Inventory, Nekoyume.Model.Item.Inventory> onUpdateInventory,
            List<(ItemType type, Predicate<InventoryItem>)> predicateList,
            bool reverseOrder)
        {
            SetAction(clickItem);
            SetInventoryTab(predicateList, onUpdateInventory:onUpdateInventory, reverseOrder:reverseOrder);
            _toggleGroup.DisabledFunc = () => true;
            StartCoroutine(CoUpdateEquipped());
        }

        private IEnumerator CoUpdateEquipped()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            UpdateEquipped();
            var bestItems = GetUsableBestEquipments();
            UpdateEquipmentNotification(bestItems);
        }

        public void UpdateEquipped()
        {
            var equipments = new List<Guid>();
            for (var i = 1; i < (int)BattleType.End; i++)
            {
                var battleType = (BattleType)i;
                equipments.AddRange(States.Instance.CurrentItemSlotStates[battleType].Equipments);
            }

            foreach (var eps in _equipments.Values)
            {
                foreach (var equipment in eps)
                {
                    var equipped = equipments.Exists(x => x == ((Equipment)equipment.ItemBase).ItemId);
                    equipment.Equipped.SetValueAndForceNotify(equipped);
                }
            }
        }

        public void ClearSelectedItem()
        {
            _selectedModel?.Selected.SetValueAndForceNotify(false);
            _selectedModel = null;
            ClearFocus();
        }

        public void UpdateRunes(List<RuneState> equippedRuneState, BattleType battleType, RuneListSheet sheet)
        {
            UpdateRuneEquipped(equippedRuneState, battleType, sheet);
            UpdateRuneNotification(GetBestRunes(battleType));
            var models = GetModels(_activeTabType);
            scroll.UpdateData(models, resetScrollOnEnable);
        }

        public void UpdateFungibleAssets()
        {
            _fungibleAssets.Clear();
            foreach (var fav in States.Instance.CurrentAvatarBalances.Values)
            {
                _fungibleAssets.Add(new InventoryItem(fav));
            }
            scroll.UpdateData(_fungibleAssets, resetScrollOnEnable);
        }

        public void UpdateCostumes(List<Guid> costumes)
        {
            UpdateCostumeEquipped(costumes);
            var bestItem = GetUsableBestCostumes();
            UpdateCostumeNotification(costumes, bestItem);
        }

        public void UpdateEquipments(List<Guid> equipments)
        {
            UpdateEquipmentEquipped(equipments);

            var bestItems = GetUsableBestEquipments();
            UpdateEquipmentNotification(bestItems);
        }

        public void UpdateConsumables(List<int> consumables)
        {
            foreach (var consumable in _consumables)
            {
                var equipped = consumables.Exists(x => x == consumable.ItemBase.Id);
                consumable.Equipped.SetValueAndForceNotify(equipped);
            }
        }

        private void UpdateRuneEquipped(
            List<RuneState> runeStates,
            BattleType battleType,
            RuneListSheet sheet)
        {
            foreach (var rune in _runes)
            {
                var equipped = runeStates.Any(x => x.RuneId == rune.RuneState.RuneId);
                rune.Equipped.SetValueAndForceNotify(equipped);

                if (!sheet.TryGetValue(rune.RuneState.RuneId, out var row))
                {
                    continue;
                }

                var equippable = battleType.IsEquippableRune((RuneUsePlace)row.UsePlace);
                rune.DimObjectEnabled.SetValueAndForceNotify(!equippable);
            }
        }

        private void UpdateCostumeEquipped(List<Guid> costumes)
        {
            foreach (var costume in _costumes)
            {
                var equipped = costumes.Exists(x => x == ((Costume)costume.ItemBase).ItemId);
                costume.Equipped.SetValueAndForceNotify(equipped);
            }
        }

        private void UpdateEquipmentEquipped(List<Guid> equipments)
        {
            foreach (var eps in _equipments.Values)
            {
                foreach (var equipment in eps)
                {
                    var equipped = equipments.Exists(x => x == ((Equipment)equipment.ItemBase).ItemId);
                    equipment.Equipped.Value = equipped;
                }
            }
        }

        public void Focus(
            ItemType itemType,
            ItemSubType subType,
            List<ElementalType> elementalTypes)
        {
            switch (itemType)
            {
                case ItemType.Equipment:
                    OnTabButtonClick(equipmentButton, InventoryTabType.Equipment);
                    break;
                case ItemType.Costume:
                    OnTabButtonClick(costumeButton, InventoryTabType.Costume);
                    break;
                case ItemType.Consumable:
                    OnTabButtonClick(consumableButton, InventoryTabType.Consumable);
                    break;
            }

            foreach (var model in GetModels(itemType))
            {
                if (model.ItemBase.ItemSubType.Equals(subType))
                {
                    if (model.ItemBase.ItemType == ItemType.Equipment)
                    {
                        if (elementalTypes.Exists(x =>
                                x.Equals(model.ItemBase.ElementalType)))
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

        public void Focus(RuneType runeType, RuneListSheet sheet)
        {
            OnTabButtonClick(runeButton, InventoryTabType.Rune);
            foreach (var rune in _runes)
            {
                if (sheet.TryGetValue(rune.RuneState.RuneId, out var row))
                {
                    rune.Focused.Value = (RuneType)row.RuneType == runeType;
                }
                else
                {
                    rune.Focused.Value = false;
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

            foreach (var rune in _runes)
            {
                rune.Focused.Value = false;
            }

            foreach (var fa in _fungibleAssets)
            {
                fa.Focused.Value = false;
            }
        }

        public bool TryGetModel(ItemBase itemBase, out InventoryItem result)
        {
            result = null;
            if (itemBase is null)
            {
                return false;
            }

            if (itemBase.ItemType == ItemType.Consumable)
            {
                return TryGetConsumable(itemBase.Id, out result);
            }

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

        public bool TryGetModel(int runeId, out InventoryItem result)
        {
            result = _runes.FirstOrDefault(x => x.RuneState.RuneId == runeId);
            return result != null;
        }

        #region For tutorial

        public bool TryGetFirstCell(out InventoryItem cell)
        {
            var result = scroll.TryGetFirstItem(out cell);
            OnClickItem(cell);
            return result;
        }

        public bool TryGetCellByIndex(int index, out InventoryCell cell)
        {
            return scroll.TryGetCellByIndex(index, out cell);
        }
        #endregion
    }
}
