using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using Material = Nekoyume.Model.Item.Material;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class EnhancementInventory : MonoBehaviour
    {
        private enum Elemental
        {
            All,
            Normal,
            Fire,
            Water,
            Land,
            Wind
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

        private readonly Dictionary<ItemSubType, List<EnhancementInventoryItem>> _equipments = new();

        private readonly ReactiveProperty<ItemSubType> _selectedItemSubType = new(ItemSubType.Weapon);

        /// <summary>선택된 등급 번호. 0 = 전체.</summary>
        private readonly ReactiveProperty<int> _grade = new(0);

        /// <summary>드롭다운 인덱스 → 등급 번호. 0번은 항상 "전체"(0).</summary>
        private int[] _gradeOptions = { 0 };

        private readonly ReactiveProperty<Elemental> _elemental = new(Elemental.All);

        private readonly List<IDisposable> _disposables = new();

        private EnhancementInventoryItem _selectedModel;
        private EnhancementInventoryItem _baseModel;
        private readonly List<EnhancementInventoryItem> _materialModels = new();

        private Action<EnhancementInventoryItem, RectTransform> _onSelectItem;

        private Action<EnhancementInventoryItem, List<EnhancementInventoryItem>> _onUpdateView;

        public const int MaxMaterialCount = 50;

        private void Awake()
        {
            foreach (var categoryToggle in categoryToggles)
            {
                categoryToggle.Toggle.onValueChanged.AddListener(value =>
                {
                    if (!value)
                    {
                        return;
                    }

                    AudioController.PlayClick();
                    _selectedItemSubType.Value = categoryToggle.Type;
                });
            }

            BuildGradeFilter();

            gradeFilter.onValueChanged.AsObservable()
                .Select(GradeOptionToGrade)
                .Subscribe(grade => _grade.Value = grade)
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

        /// <summary>
        /// 등급 드롭다운을 시트의 실제 장비 등급으로 구성한다.
        /// </summary>
        /// <remarks>
        /// 예전에는 등급마다 로컬 enum 멤버를 더해야 했고, 빠뜨리면 그 등급 장비를
        /// 걸러볼 수 없었다(등급 9 "Ultimate" 가 실제로 그렇게 빠졌다).
        /// 등급 목록의 권위는 <c>EquipmentItemSheet</c> 이다 — 이 필터가 장비 전용이므로
        /// 아이템 전체가 아니라 장비 등급만 본다. grade 0(초기 지급 장비)은 등급 체계 밖이다.
        /// </remarks>
        private void BuildGradeFilter()
        {
            var grades = new SortedSet<int>();
            var sheet = Game.Game.instance?.TableSheets?.EquipmentItemSheet;
            if (sheet is not null)
            {
                foreach (var row in sheet.Values)
                {
                    if (row.Grade > 0)
                    {
                        grades.Add(row.Grade);
                    }
                }
            }
            else
            {
                NcDebug.LogWarning(
                    $"{nameof(EnhancementInventory)}: 시트를 읽지 못해 등급 필터를 전체만 둡니다.");
            }

            // 0번은 "전체"(UI_ITEM_GRADE_0). 나머지는 등급 번호 오름차순.
            var options = new List<int> { 0 };
            options.AddRange(grades);
            _gradeOptions = options.ToArray();

            gradeFilter.ClearOptions();
            gradeFilter.AddOptions(_gradeOptions
                .Select(grade => L10nManager.Localize($"UI_ITEM_GRADE_{grade}"))
                .ToList());
        }

        /// <summary>드롭다운 인덱스를 등급 번호로. 범위를 벗어나면 전체(0).</summary>
        private int GradeOptionToGrade(int index)
        {
            return index >= 0 && index < _gradeOptions.Length ? _gradeOptions[index] : 0;
        }

        public (Equipment, List<Equipment>, Dictionary<int, int>) GetSelectedModels()
        {
            var baseItem = (Equipment)_baseModel?.ItemBase;
            var materialItems = _materialModels
                .Select(item => item.ItemBase).OfType<Equipment>().ToList();
            var hammers = _materialModels
                .Where(item => ItemEnhancement.HammerIds.Contains(item.ItemBase.Id))
                .ToDictionary(item => item.ItemBase.Id, item => item.SelectedMaterialCount.Value);

            return (baseItem, materialItems, hammers);
        }

#region Select Item

        private void SetMaterialItemCount(EnhancementInventoryItem item, int count)
        {
            item.SelectedMaterialCount.Value = count;
            if (count > 0)
            {
                if (!_materialModels.Contains(item))
                {
                    _materialModels.Add(item);
                }
            }
            else
            {
                _materialModels.Remove(item);
            }

            UpdateView();
        }

        private void SelectMaterialItem(EnhancementInventoryItem item)
        {
            SetMaterialItemCount(item, 1);
        }

        private void DeselectMaterialItem(EnhancementInventoryItem item)
        {
            SetMaterialItemCount(item, 0);
        }

        private void SelectBaseItem(EnhancementInventoryItem item)
        {
            _baseModel = item;
            _baseModel.SelectedBase.SetValueAndForceNotify(true);
            UpdateView();
        }

        public void DeselectBaseItem()
        {
            _baseModel?.SelectedBase.SetValueAndForceNotify(false);
            _baseModel = null;

            DeselectAllMaterialItems();
        }

        public void AutoSelectMaterialItems(int amount)
        {
            if (_baseModel is null)
            {
                return;
            }

            var models = GetModels();
            models.Reverse();
            var count = 0;
            foreach (var model in models.Where(model =>
                model.SelectedMaterialCount.Value <= 0 &&
                !ItemEnhancement.HammerIds.Contains(model.ItemBase.Id) &&
                !model.Disabled.Value &&
                !model.Equals(_baseModel) &&
                !model.Equipped.Value))
            {
                if (_materialModels.Count >= MaxMaterialCount)
                {
                    break;
                }

                if (count >= amount)
                {
                    break;
                }

                SelectMaterialItem(model);
                count++;
            }
        }

        public void DeselectAllMaterialItems()
        {
            foreach (var model in _materialModels)
            {
                model.SelectedMaterialCount.Value = 0;
            }

            _materialModels.Clear();

            UpdateView();
        }

#endregion

        public void Select(ItemSubType itemSubType, Guid itemId)
        {
            var toggle = categoryToggles.FirstOrDefault(x => x.Type == itemSubType);
            toggle.Toggle.isOn = true;

            var items = _equipments[itemSubType];
            var item = items.First(item =>
                item.ItemBase is Equipment equipment && equipment.ItemId == itemId);

            if (_baseModel is null)
            {
                SelectBaseItem(item);
            }
            else if (!item.Disabled.Value)
            {
                SelectMaterialItem(item);
            }
        }

        public void Select(ItemSheet.Row row)
        {
            var toggle = categoryToggles.FirstOrDefault(x => x.Type == row.ItemSubType);
            toggle.Toggle.isOn = true;

            var items = _equipments[row.ItemSubType];
            var item = items.First(item => item.ItemBase.Id == row.Id);

            if (_baseModel is null)
            {
                SelectBaseItem(item);
            }
            else if (!item.Disabled.Value)
            {
                SelectMaterialItem(item);
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
            if (item.Equals(_baseModel))
            {
                DeselectBaseItem();
            }
            else if (_materialModels.Contains(item))
            {
                DeselectMaterialItem(item);
            }
            else if (_baseModel is null)
            {
                SelectBaseItem(item);
            }
            else if (!item.Disabled.Value)
            {
                SelectMaterialItem(item);
            }
        }

        private void OnClickHammerItem(EnhancementInventoryItem item)
        {
            // Hammer isn't selected to base item
            if (_baseModel is null)
            {
                return;
            }

            if (item.Disabled.Value)
            {
                return;
            }

            Widget.Find<AddHammerPopup>().Show(
                _baseModel.ItemBase as Equipment,
                _materialModels,
                item,
                count => SetMaterialItemCount(item, count));
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
                        item.Disabled.Value = ItemEnhancement.HammerIds.Contains(item.ItemBase.Id);
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
                            (item.ItemBase.ItemSubType != baseItemSubType &&
                                !(enableHammer && ItemEnhancement.HammerIds.Contains(item.ItemBase.Id)));
                    }
                }
            }
        }

        private List<EnhancementInventoryItem> GetModels()
        {
            if (!_equipments.TryGetValue(_selectedItemSubType.Value, out var equipments))
            {
                equipments = new List<EnhancementInventoryItem>();
            }

            if (_grade.Value > 0)
            {
                var value = _grade.Value;
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
            scroll.OnClick.Subscribe(item =>
            {
                if (ItemEnhancement.HammerIds.Contains(item.ItemBase.Id))
                {
                    OnClickHammerItem(item);
                }
                else
                {
                    OnClickItem(item);
                }
            }).AddTo(_disposables);
            enhancementSelectedMaterialItemScroll.OnClick.Subscribe(item =>
            {
                if (ItemEnhancement.HammerIds.Contains(item.ItemBase.Id))
                {
                    DeselectMaterialItem(item);
                }
                else
                {
                    OnClickItem(item);
                }
            }).AddTo(_disposables);
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
                    case ItemType.Material when
                        ItemEnhancement.HammerIds.Contains(item.item.Id):
                        AddItem(item.item, item.count);
                        break;
                }
            }

            _baseModel = null;
            _materialModels.Clear();

            UpdateEquipmentEquipped();

            UpdateView(resetScrollOnEnable);
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

            EnhancementInventoryItem inventoryItem;
            switch (itemBase)
            {
                case Equipment equipment:
                    inventoryItem = new EnhancementInventoryItem(
                        itemBase, equipment.equipped, !Util.IsUsableItem(itemBase), count);
                    break;
                case Material:
                    inventoryItem = new EnhancementInventoryItem(itemBase, false, false, count);
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

        private void UpdateEquipmentEquipped()
        {
            var equippedEquipments = new List<Guid>();
            for (var i = 1; i < (int)BattleType.End; i++)
            {
                equippedEquipments.AddRange(States.Instance.CurrentItemSlotStates[(BattleType)i].Equipments);
            }

            foreach (var equipments in _equipments
                .Where(pair => pair.Key != ItemSubType.EquipmentMaterial)
                .Select(pair => pair.Value))
            {
                foreach (var equipment in equipments)
                {
                    var equipped = equippedEquipments.Contains(((Equipment)equipment.ItemBase).ItemId);
                    equipment.Equipped.Value = equipped;
                }
            }
        }

#endregion
    }
}
