using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class EnhancementInventory : MonoBehaviour
    {
        private enum Grade
        {
            All,
            Normal,
            Rare,
            Epic,
            Unique,
            Legend,
            Divinity
        }

        private enum Elemental
        {
            All,
            Normal,
            Fire,
            Water,
            Land,
            Wind,
        }

        [Serializable]
        private struct CategoryToggle
        {
            public Toggle Toggle;
            public ItemSubType Type;
        }

        [SerializeField]
        private List<CategoryToggle> categoryToggles;

        [SerializeField]
        private TMP_Dropdown gradeFilter;

        [SerializeField]
        private TMP_Dropdown elementalFilter;

        [SerializeField]
        private EnhancementInventoryScroll scroll;

        [SerializeField]
        private bool resetScrollOnEnable;

        [SerializeField]
        private RectTransform tooltipSocket;

        private readonly Dictionary<ItemSubType, List<EnhancementInventoryItem>> _equipments =
            new Dictionary<ItemSubType, List<EnhancementInventoryItem>>();

        private readonly ReactiveProperty<ItemSubType> _selectedItemSubType =
            new ReactiveProperty<ItemSubType>(ItemSubType.Weapon);

        private readonly ReactiveProperty<Grade> _grade =
            new ReactiveProperty<Grade>(Grade.All);

        private readonly ReactiveProperty<Elemental> _elemental =
            new ReactiveProperty<Elemental>(Elemental.All);

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private EnhancementInventoryItem _selectedModel;
        private EnhancementInventoryItem _baseModel;
        private readonly List<EnhancementInventoryItem> _materialModels = new List<EnhancementInventoryItem>();

        private Action<EnhancementInventoryItem, RectTransform> _onSelectItem;

        private Action<EnhancementInventoryItem, List<EnhancementInventoryItem>> _onUpdateView;

        public const int MaxMaterialCount = 50;

        private void Awake()
        {
            foreach (var categoryToggle in categoryToggles)
            {
                categoryToggle.Toggle.onValueChanged.AddListener(value =>
                {
                    if (!value) return;
                    AudioController.PlayClick();
                    _selectedItemSubType.Value = categoryToggle.Type;
                });
            }

            gradeFilter.AddOptions((
                from grade in (Grade[])Enum.GetValues(typeof(Grade))
                select L10nManager.Localize($"UI_ITEM_GRADE_{(int)grade}")).ToList());

            gradeFilter.onValueChanged.AsObservable()
                .Select(index => (Grade)index)
                .Subscribe(filter => _grade.Value = filter)
                .AddTo(gameObject);

            elementalFilter.AddOptions((
                from elemental in Enum.GetNames(typeof(Elemental))
                select L10nManager.Localize($"ELEMENTAL_TYPE_{elemental.ToUpper()}")).ToList());

            elementalFilter.onValueChanged.AsObservable()
                .Select(index => (Elemental)index)
                .Subscribe(filter => _elemental.Value = filter)
                .AddTo(gameObject);

            _grade.Subscribe(_ => UpdateView(true)).AddTo(gameObject);
            _elemental.Subscribe(_ => UpdateView(true)).AddTo(gameObject);
            _selectedItemSubType.Subscribe(_ => UpdateView(true)).AddTo(gameObject);
        }

        public (Equipment, List<Equipment>) GetSelectedModels()
        {
            var baseItem = (Equipment)_baseModel?.ItemBase;
            var materialItems = _materialModels.Select(item => (Equipment)item.ItemBase).ToList();

            return (baseItem, materialItems);
        }

        #region Select Item

        private void ClearSelectedItem()
        {
            _selectedModel?.Selected.SetValueAndForceNotify(false);
            _selectedModel = null;
        }

        private void SelectItem(EnhancementInventoryItem item)
        {
            if (_baseModel is null)
            {
                _baseModel = item;
                _baseModel.SelectedBase.SetValueAndForceNotify(true);
            }
            else
            {
                if (item.Disabled.Value)
                {
                    return;
                }

                item.SelectedMaterial.SetValueAndForceNotify(true);
                _materialModels.Add(item);
            }

            UpdateView();
        }

        private void DeselectMaterialItem(EnhancementInventoryItem item)
        {
            item.SelectedMaterial.SetValueAndForceNotify(false);
            _materialModels.Remove(item);
            UpdateView();
        }

        public void DeselectBaseItem()
        {
            _baseModel?.SelectedBase.SetValueAndForceNotify(false);
            _baseModel = null;

            DeselectAllMaterialItems();
        }

        public void DeselectAllMaterialItems()
        {
            foreach (var model in _materialModels)
            {
                model?.SelectedMaterial.SetValueAndForceNotify(false);
            }

            _materialModels.Clear();

            UpdateView();
        }

        #endregion

        public void Select(ItemSubType itemSubType, Guid itemId)
        {
            var items = _equipments[itemSubType];
            foreach (var item in items)
            {
                if (item.ItemBase is not Equipment equipment)
                {
                    continue;
                }

                if (equipment.ItemId != itemId)
                {
                    continue;
                }

                var toggle = categoryToggles.FirstOrDefault(x => x.Type == itemSubType);
                toggle.Toggle.isOn = true;
                ClearSelectedItem();
                _selectedModel = item;
                // _selectedModel.Selected.SetValueAndForceNotify(true);
                SelectItem(_selectedModel);
                return;
            }
        }

        public EnhancementInventoryItem GetEnabledItem(int index)
        {
            return GetModels().ElementAt(index);
        }

        public bool TryGetCellByIndex(int index, out EnhancementInventoryCell cell)
        {
            return scroll.TryGetCellByIndex(index, out cell);
        }

        private void OnClickItem(EnhancementInventoryItem item)
        {
            ClearSelectedItem();

            if (_baseModel.Equals(item))
            {
                DeselectBaseItem();
                return;
            }

            if (_materialModels.Contains(item))
            {
                DeselectMaterialItem(item);
                return;
            }

            SelectItem(item);
        }

        private void UpdateView(bool jumpToFirst = false)
        {
            var models = GetModels();
            DisableItem(models);
            _onUpdateView?.Invoke(_baseModel, _materialModels);
            scroll.UpdateData(models, jumpToFirst);
            return;

            void DisableItem(IEnumerable<EnhancementInventoryItem> items)
            {
                if (_baseModel is null)
                {
                    foreach (var item in items)
                    {
                        item.Disabled.Value = false;
                    }
                }
                else
                {
                    var baseItemSubType = _baseModel.ItemBase.ItemSubType;
                    var fullOfMaterials = _materialModels.Count >= MaxMaterialCount;
                    var enableHammer = !ItemEnhancement.HammerBannedTypes.Contains(baseItemSubType);
                    foreach (var item in items)
                    {
                        item.Disabled.Value = fullOfMaterials ||
                            item.ItemBase.ItemSubType != baseItemSubType &&
                            !(enableHammer && ItemEnhancement.HammerIds.Contains(item.ItemBase.Id));
                    }
                }
            }
        }

        private List<EnhancementInventoryItem> GetModels()
        {
            if (!_equipments.TryGetValue(_selectedItemSubType.Value, out var equipments))
            {
                return new List<EnhancementInventoryItem>();
            }

            if (_grade.Value != Grade.All)
            {
                var value = (int)_grade.Value;
                equipments = equipments.Where(item => item.ItemBase.Grade == value).ToList();
            }

            if (_elemental.Value != Elemental.All)
            {
                var value = (int)_elemental.Value - 1;
                equipments = equipments.Where(item => (int)item.ItemBase.ElementalType == value).ToList();
            }

            var usableItems = new List<EnhancementInventoryItem>();
            var unusableItems = new List<EnhancementInventoryItem>();
            foreach (var item in equipments)
            {
                if (Util.IsUsableItem(item.ItemBase))
                {
                    usableItems.Add(item);
                }
                else
                {
                    unusableItems.Add(item);
                }
            }

            if (usableItems.Any())
            {
                usableItems = usableItems
                    .OrderByDescending(x => x.ItemBase.Grade)
                    .ThenByDescending(x => CPHelper.GetCP(x.ItemBase as Equipment)).ToList();
            }

            var result = new List<EnhancementInventoryItem>();
            if (!ItemEnhancement.HammerBannedTypes.Contains(_selectedItemSubType.Value) &&
                _equipments.TryGetValue(ItemSubType.EquipmentMaterial, out var hammers))
            {
                result.AddRange(hammers);
            }

            result.AddRange(usableItems);
            result.AddRange(unusableItems);

            return result;
        }

        public void Set(Action<EnhancementInventoryItem, List<EnhancementInventoryItem>> onUpdateView,
            EnhancementSelectedMaterialItemScroll enhancementSelectedMaterialItemScroll)
        {
            _onUpdateView = onUpdateView;

            _disposables.DisposeAllAndClear();
            ReactiveAvatarState.Inventory.Subscribe(UpdateInventory).AddTo(_disposables);
            scroll.OnClick.Subscribe(OnClickItem).AddTo(_disposables);
            enhancementSelectedMaterialItemScroll.OnClick.Subscribe(OnClickItem).AddTo(_disposables);
        }

        #region Update Inventory

        private void UpdateInventory(Nekoyume.Model.Item.Inventory inventory)
        {
            _equipments.Clear();
            if (inventory is null)
            {
                return;
            }

            foreach (var item in inventory.Items)
            {
                if (item.Locked)
                {
                    continue;
                }

                switch (item.item.ItemType)
                {
                    case ItemType.Equipment:
                        AddItem(item.item);
                        break;
                }
            }

            _selectedModel = null;
            _baseModel = null;
            _materialModels.Clear();

            UpdateView(resetScrollOnEnable);
        }

        private void AddItem(ItemBase itemBase)
        {
            if (itemBase is ITradableItem tradableItem)
            {
                var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;
                if (tradableItem.RequiredBlockIndex > blockIndex)
                {
                    return;
                }
            }

            EnhancementInventoryItem inventoryItem;
            switch (itemBase)
            {
                case Equipment equipment:
                    inventoryItem = new EnhancementInventoryItem(
                        itemBase, equipment.equipped, !Util.IsUsableItem(itemBase));
                    break;
                default:
                    return;
            }

            if (!_equipments.ContainsKey(inventoryItem.ItemBase.ItemSubType))
            {
                _equipments.Add(
                    inventoryItem.ItemBase.ItemSubType,
                    new List<EnhancementInventoryItem>());
            }

            _equipments[inventoryItem.ItemBase.ItemSubType].Add(inventoryItem);
        }

        #endregion
    }
}
