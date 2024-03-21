using System;
using System.Collections.Generic;
using System.Linq;
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
        private List<CategoryToggle> categoryToggles = null;

        [SerializeField]
        private TMP_Dropdown gradeFilter = null;

        [SerializeField]
        private TMP_Dropdown elementalFilter = null;

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
        private List<EnhancementInventoryItem> _materialModels = new List<EnhancementInventoryItem>();

        private Action<EnhancementInventoryItem, RectTransform> _onSelectItem;

        private Action<EnhancementInventoryItem, List<EnhancementInventoryItem>> _onUpdateView;

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
            var materialItems = _materialModels.Select((i) => (Equipment)i.ItemBase).ToList();

            return (baseItem, materialItems);
        }

        public string GetSubmitText()
        {
            return L10nManager.Localize(_baseModel is null
                ? "UI_COMBINATION_REGISTER_ITEM"
                : "UI_COMBINATION_REGISTER_MATERIAL");
        }

        public void ClearSelectedItem()
        {
            _selectedModel?.Selected.SetValueAndForceNotify(false);
            _selectedModel = null;
        }

        public void SelectItem()
        {
            if (_baseModel is null)
            {
                _baseModel = _selectedModel;
                _baseModel.SelectedBase.SetValueAndForceNotify(true);
            }
            else
            {
                _selectedModel.SelectedMaterial.SetValueAndForceNotify(true);
                _materialModels.Add(_selectedModel);
            }

            UpdateView();
        }

        public void DeselectItem(bool isAll = false)
        {
            if (isAll)
            {
                _baseModel?.SelectedBase.SetValueAndForceNotify(false);
                _baseModel = null;
            }

            ClearAllMaterialModels();

            UpdateView();
        }

        private void ClearAllMaterialModels()
        {
            foreach (var model in _materialModels)
            {
                model?.SelectedMaterial.SetValueAndForceNotify(false);
            }
            _materialModels.Clear();
        }

        public void Select(ItemSubType itemSubType,Guid itemId)
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
                SelectItem();
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

            if (item.Equals(_baseModel)) // 둘다 해제
            {
                _baseModel.SelectedBase.SetValueAndForceNotify(false);
                _baseModel = null;

                ClearAllMaterialModels();

                UpdateView();
                return;
            }

            foreach (var model in _materialModels)
            {
                if (item.Equals(model))
                {
                    model.SelectedMaterial.SetValueAndForceNotify(false);
                    _materialModels.Remove(model);
                    UpdateView();
                    return;
                }
            }


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

        private void DisableItem(IEnumerable<EnhancementInventoryItem> items)
        {
            if (_baseModel is null)
            {
                foreach (var model in items)
                {
                    model.Disabled.Value = false;
                }
            }
            else
            {
                foreach (var model in items)
                {
                    model.Disabled.Value = IsDisable(_baseModel, model);
                }
            }
        }

        private bool IsDisable(EnhancementInventoryItem a, EnhancementInventoryItem b)
        {
            return a.ItemBase.ItemSubType != b.ItemBase.ItemSubType || _materialModels.Count >= 50;
        }

        private void UpdateView(bool jumpToFirst = false)
        {
            var models = GetModels();
            DisableItem(models);
            _onUpdateView?.Invoke(_baseModel, _materialModels);
            scroll.UpdateData(models, jumpToFirst);
        }

        private List<EnhancementInventoryItem> GetModels()
        {
            if (!_equipments.ContainsKey(_selectedItemSubType.Value))
            {
                return new List<EnhancementInventoryItem>();
            }

            var equipments = _equipments[_selectedItemSubType.Value].ToList();
            if (_grade.Value != Grade.All)
            {
                var value = (int)(_grade.Value);
                equipments = equipments.Where(x => x.ItemBase.Grade == value).ToList();
            }

            if (_elemental.Value != Elemental.All)
            {
                var value = (int)_elemental.Value - 1;
                equipments = equipments.Where(x => (int)x.ItemBase.ElementalType == value).ToList();
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
                var bestItem = usableItems
                    .OrderByDescending(x => CPHelper.GetCP(x.ItemBase as Equipment)).First();
                usableItems = usableItems.OrderByDescending(x => x.Equals(bestItem))
                    .ToList();
            }

            usableItems.AddRange(unusableItems);
            return usableItems;
        }

        public void Set(Action<EnhancementInventoryItem, List<EnhancementInventoryItem>> onUpdateView,
            EnhancementSelectedMaterialItemScroll enhancementSelectedMaterialItemScroll)
        {
            _onUpdateView = onUpdateView;

            _disposables.DisposeAllAndClear();
            ReactiveAvatarState.Inventory.Subscribe(inventory =>
            {
                _equipments.Clear();

                if (inventory is null)
                {
                    return;
                }

                _selectedModel = null;
                foreach (var item in inventory.Items)
                {
                    if (!(item.item is Equipment) || item.Locked)
                    {
                        continue;
                    }

                    AddItem(item.item);
                }

                _baseModel = null;
                _materialModels.Clear();

                UpdateView(resetScrollOnEnable);
            }).AddTo(_disposables);

            scroll.OnClick.Subscribe(OnClickItem).AddTo(_disposables);
            enhancementSelectedMaterialItemScroll.OnClick.Subscribe(OnClickItem).AddTo(_disposables);
        }

        private void AddItem(ItemBase itemBase)
        {
            var equipment = (Equipment)itemBase;
            if (itemBase is ITradableItem tradableItem)
            {
                var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;
                if (tradableItem.RequiredBlockIndex > blockIndex)
                {
                    return;
                }
            }

            var inventoryItem = new EnhancementInventoryItem(itemBase,
                equipped: equipment.equipped,
                levelLimited: !Util.IsUsableItem(itemBase));

            if (!_equipments.ContainsKey(inventoryItem.ItemBase.ItemSubType))
            {
                _equipments.Add(inventoryItem.ItemBase.ItemSubType,
                    new List<EnhancementInventoryItem>());
            }

            _equipments[inventoryItem.ItemBase.ItemSubType].Add(inventoryItem);
        }
    }
}
