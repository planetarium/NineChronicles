#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UnityEngine;
using Inventory = Nekoyume.Model.Item.Inventory;

namespace Nekoyume.UI
{
    using UniRx;

    public class Synthesis : Widget
    {
        private const ItemSubType DefaultItemSubType = ItemSubType.Aura;

        private readonly List<IDisposable> _activeDisposables = new();
        private readonly ToggleGroup _toggleGroup = new();

        [Serializable]
        private struct SynthesizeTapGroup
        {
            public ItemSubType iemSubType;
            public CategoryTabButton tabButton;
        }

        #region SerializeField

        [SerializeField]
        private SynthesisModule synthesisModule = null!;

        [SerializeField]
        private SynthesisScroll synthesisScroll = null!;

        [SerializeField]
        private SynthesizeTapGroup[] synthesisTapGroup = null!;

        #endregion SerializeField

        #region Field

        private ItemSubType _currentItemSubType = DefaultItemSubType;

        private Inventory? _cachedInventory;

        private readonly List<SynthesizeModel> _gradeItems = new();

        #endregion Field

        #region

        private ItemSubType CurrentItemSubType
        {
            get => _currentItemSubType;
            set
            {
                if (_currentItemSubType == value)
                {
                    return;
                }

                _currentItemSubType = value;
                UpdateItems();
            }
        }

        #endregion Properties

        #region MonoBehaviour

        protected override void Awake()
        {
            CheckNull();
            base.Awake();

            foreach (var tapGroup in synthesisTapGroup)
            {
                var tapButton = tapGroup.tabButton;
                _toggleGroup.RegisterToggleable(tapButton);
                tapButton.OnClick.Subscribe(_ =>
                {
                    CurrentItemSubType = tapGroup.iemSubType;
                }).AddTo(gameObject);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            ReactiveAvatarState.Inventory
                               .Subscribe(UpdateInventory)
                               .AddTo(_activeDisposables);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _activeDisposables.DisposeAllAndClear();
        }

        #endregion MonoBehaviour

        #region Widget

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            CurrentItemSubType = DefaultItemSubType;
        }

        #endregion Widget

        #region PrivateUtils

        private void CheckNull()
        {
            if (synthesisModule == null)
            {
                throw new NullReferenceException("activeBackgroundObject is null");
            }
        }

        private void UpdateInventory(Inventory inventory)
        {
            _cachedInventory = inventory;
            UpdateItems();
        }

        private readonly Dictionary<Grade, int> _gradeItemCountDict = new ();
        /// <summary>
        /// When updating the items, inventory should be updated or change the item sub type.
        /// </summary>
        private void UpdateItems()
        {
            if (_cachedInventory == null)
            {
                NcDebug.LogWarning($"[{nameof(Synthesis)} inventory is null");
                return;
            }

            var itemList = _cachedInventory.Items.Where(CheckInventoryItemSubType)
                                           .ToList();
            FillGradeItemCountDict(itemList);

            _gradeItems.Clear();
            foreach (var kvp in _gradeItemCountDict)
            {
                var grade = kvp.Key;
                var inventoryItemCount = kvp.Value;

                // TODO: 아이템 타입별로 숫자 달라짐
                var sheet = TableSheets.Instance.SynthesizeSheet;
                var row = sheet.Values.FirstOrDefault(row => row.GradeId == (int)grade);
                if (row == null)
                {
                    // 특정 grade에 대한 row가 없을 수 있음
                    continue;
                }

                var requiredItemCount = row.RequiredCount;
                var model = new SynthesizeModel(grade, inventoryItemCount, requiredItemCount);
                _gradeItems.Add(model);
            }
        }

        private bool CheckInventoryItemSubType(Inventory.Item item) => item.item.ItemSubType == CurrentItemSubType;

        private void FillGradeItemCountDict(List<Inventory.Item> itemList)
        {
            _gradeItemCountDict.Clear();
            foreach (var item in itemList)
            {
                if (_gradeItemCountDict.ContainsKey((Grade)item.item.Grade))
                {
                    _gradeItemCountDict[(Grade)item.item.Grade]++;
                }
                else
                {
                    _gradeItemCountDict[(Grade)item.item.Grade] = 1;
                }
            }
        }

        #endregion PrivateUtils

        #region Utils

        public static HashSet<int>? GetSynthesizeResultPool(Grade grade, ItemSubType itemSubType)
        {
            switch (itemSubType)
            {
                case ItemSubType.Aura:
                case ItemSubType.Grimoire:
                    var equipmentItem = TableSheets.Instance.EquipmentItemSheet;
                    return SynthesizeSimulator.GetSynthesizeResultPool(grade, itemSubType, equipmentItem);
                case ItemSubType.FullCostume:
                case ItemSubType.Title:
                    var costumeItem = TableSheets.Instance.CostumeItemSheet;
                    return SynthesizeSimulator.GetSynthesizeResultPool(grade, itemSubType, costumeItem);
            }

            return null;
        }

        #endregion Utils
    }
}
