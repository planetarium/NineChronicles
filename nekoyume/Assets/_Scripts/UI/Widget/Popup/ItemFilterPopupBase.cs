using System;
using System.Collections.Generic;
using Nekoyume.Model.Item;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public struct ItemFilterOptions
    {
        public ItemFilterPopupBase.Grade Grade;
        public ItemFilterPopupBase.Elemental Elemental;
        public ItemFilterPopupBase.ItemType ItemType;
        public ItemFilterPopupBase.UpgradeLevel UpgradeLevel;
        public ItemFilterPopupBase.OptionCount OptionCount;
        public ItemFilterPopupBase.WithSkill WithSkill;

        public string SearchText;

        public bool IsNeedFilter => Grade != ItemFilterPopupBase.Grade.All ||
                                      Elemental != ItemFilterPopupBase.Elemental.All ||
                                      ItemType != ItemFilterPopupBase.ItemType.All ||
                                      UpgradeLevel != ItemFilterPopupBase.UpgradeLevel.All ||
                                      OptionCount != ItemFilterPopupBase.OptionCount.All ||
                                      WithSkill != ItemFilterPopupBase.WithSkill.All;
    }

    public abstract class ItemFilterPopupBase : PopupWidget
    {
        #region Internal Type
        /// <summary>
        /// 아무것도 선택하지 않은 상태가 필터링을 하지 않아 전체 아이템을 보여주는 것으로 간주한다.
        /// </summary>
        [Flags]
        public enum Grade
        {
            All = 0,
            Normal = 1 << 0,
            Rare = 1 << 1,
            Epic = 1 << 2,
            Unique = 1 << 3,
            Legendary = 1 << 4,
            Divinity = 1 << 5,
        }

        [Flags]
        public enum Elemental
        {
            All = 0,
            Normal = 1 << 0,
            Fire = 1 << 1,
            Water = 1 << 2,
            Land = 1 << 3,
            Wind = 1 << 4,
        }

        [Flags]
        public enum ItemType
        {
            All = 0,
            Weapon = 1 << 0,
            Armor = 1 << 1,
            Belt = 1 << 2,
            Necklace = 1 << 3,
            Ring = 1 << 4,
            Aura = 1 << 5,
        }

        [Flags]
        public enum UpgradeLevel
        {
            All = 0,
            Level0 = 1 << 0,
            Level1 = 1 << 1,
            Level2 = 1 << 2,
            Level3 = 1 << 3,
            Level4 = 1 << 4,
            Level5More = 1 << 5,
        }

        [Flags]
        public enum OptionCount
        {
            All = 0,
            One = 1 << 0,
            Two = 1 << 1,
            Three = 1 << 2,
        }

        [Flags]
        public enum WithSkill
        {
            All = 0,
            None = 1 << 0,
            With = 1 << 1,
        }

        [Serializable]
        private abstract class ItemToggleType
        {
            public Toggle toggle;

            public abstract bool IsAll { get; }

            public abstract string GetOptionName { get; }

            public void ResetToAll()
            {
                if (toggle.isOn != IsAll)
                    toggle.isOn = IsAll;
            }

            public void OffAllToggle()
            {
                if (IsAll && toggle.isOn)
                    toggle.isOn = false;
            }
        }

        [Serializable]
        private class GradeToggle : ItemToggleType
        {
            public Grade grade;

            public override bool IsAll => grade == Grade.All;
            public override string GetOptionName => grade.ToString();
        }

        [Serializable]
        private class ElementalToggle : ItemToggleType
        {
            public Elemental elemental;

            public override bool IsAll => elemental == Elemental.All;
            public override string GetOptionName => elemental.ToString();
        }

        [Serializable]
        private class ItemTypeToggle : ItemToggleType
        {
            public ItemType itemType;

            public override bool IsAll => itemType == ItemType.All;
            public override string GetOptionName => itemType.ToString();
        }

        [Serializable]
        private class UpgradeLevelToggle : ItemToggleType
        {
            public UpgradeLevel upgradeLevel;

            public override bool IsAll => upgradeLevel == UpgradeLevel.All;
            public override string GetOptionName => upgradeLevel.ToString();
        }

        [Serializable]
        private class OptionCountToggle : ItemToggleType
        {
            public OptionCount optionCount;

            public override bool IsAll => optionCount == OptionCount.All;
            public override string GetOptionName => optionCount.ToString();
        }

        [Serializable]
        private class WithSkillToggle : ItemToggleType
        {
            public WithSkill withSkill;

            public override bool IsAll => withSkill == WithSkill.All;
            public override string GetOptionName => withSkill.ToString();
        }

        #endregion Internal Type

        [SerializeField]
        private List<GradeToggle> gradeToggles;

        [SerializeField]
        private List<ElementalToggle> elementalToggles;

        [SerializeField]
        private List<ItemTypeToggle> itemTypeToggles;

        [SerializeField]
        private List<UpgradeLevelToggle> upgradeLevelToggles;

        [SerializeField]
        private List<OptionCountToggle> optionCountToggles;

        [SerializeField]
        private List<WithSkillToggle> withSkillToggles;

        [SerializeField]
        private TMP_InputField _searchInputField;

        [SerializeField]
        private Button _deselectAllButton;

        [SerializeField]
        private Button _okButton;

        private ItemFilterOptions _itemFilterOptions;

        #region Popup
        protected override void Awake()
        {
            base.Awake();

            InitializeToggleGroup();

            CloseWidget = () =>
            {
                if (_searchInputField.isFocused)
                    return;

                Close(true);
            };

            _deselectAllButton.onClick.AddListener(DeselectAll);
            _okButton.onClick.AddListener(OnClickOkButton);
        }
        #endregion Popup

        private void InitializeToggleGroup()
        {
            BindToggleEvent(gradeToggles);
            BindToggleEvent(elementalToggles);
            BindToggleEvent(itemTypeToggles);
            BindToggleEvent(upgradeLevelToggles);
            BindToggleEvent(optionCountToggles);
            BindToggleEvent(withSkillToggles);
        }

        private void BindToggleEvent<T>(List<T> toggles) where T : ItemToggleType
        {
            foreach (var item in toggles)
            {
                item.toggle.name = item.GetOptionName;
                var textComponent = item.toggle.GetComponentInChildren<Text>();
                if (textComponent != null)
                    textComponent.text = item.GetOptionName;

                if (item.IsAll)
                {
                    item.toggle.onValueChanged.AddListener(isOn =>
                    {
                        if (isOn) ResetToAll(toggles);
                        else if (IsOffAllToggle(toggles))
                            ResetToAll(toggles);
                    });
                }
                else
                {
                    item.toggle.onValueChanged.AddListener(isOn =>
                    {
                        if (isOn)
                            OffAllToggle(toggles);
                        else if (IsOffAllToggle(toggles))
                            ResetToAll(toggles);
                    });
                }
            }

            ResetToAll(toggles);
        }

        private bool IsOffAllToggle<T>(List<T> toggles) where T : ItemToggleType
        {
            foreach (var item in toggles)
            {
                if (item.toggle.isOn)
                    return false;
            }

            return true;
        }

        private void OffAllToggle<T>(List<T> toggles) where T : ItemToggleType
        {
            foreach (var item in toggles)
                item.OffAllToggle();
        }

        private void ResetToAll<T>(List<T> toggles) where T : ItemToggleType
        {
            foreach (var item in toggles)
                item.ResetToAll();
        }

        private void DeselectAll()
        {
            ResetToAll(gradeToggles);
            ResetToAll(elementalToggles);
            ResetToAll(itemTypeToggles);
            ResetToAll(upgradeLevelToggles);
            ResetToAll(optionCountToggles);
            ResetToAll(withSkillToggles);
            _searchInputField.text = string.Empty;
        }

        public void OnClickOkButton()
        {
            ApplyItemFilterOptionFromToggle();
            Close(true);
        }

        /// <summary>
        /// 현재 선택된 아이템 탭에 따라 적용할 필터 옵션을 활성화/비활성화 시킨다.
        /// 현재 gradeToggles를 제외한 모든 필터 토글이 Equipment 탭에서만 활성화 된다.
        /// </summary>
        /// <param name="itemType">현재 활성화된 아이템 탭</param>
        public void SetItemTypeTap(Nekoyume.Model.Item.ItemType itemType)
        {
            foreach (var elementalToggle in elementalToggles)
            {
                elementalToggle.toggle.interactable = itemType == Nekoyume.Model.Item.ItemType.Equipment;
            }

            foreach (var itemTypeToggle in itemTypeToggles)
            {
                itemTypeToggle.toggle.interactable = itemType == Nekoyume.Model.Item.ItemType.Equipment;
            }

            foreach (var upgradeLevelToggle in upgradeLevelToggles)
            {
                upgradeLevelToggle.toggle.interactable = itemType == Nekoyume.Model.Item.ItemType.Equipment;
            }

            foreach (var optionCountToggle in optionCountToggles)
            {
                optionCountToggle.toggle.interactable = itemType == Nekoyume.Model.Item.ItemType.Equipment;
            }

            foreach (var withSkillToggle in withSkillToggles)
            {
                withSkillToggle.toggle.interactable = itemType == Nekoyume.Model.Item.ItemType.Equipment;
            }
        }

        protected void ApplyItemFilterOptionFromToggle()
        {
            var itemFilterOptionType = new ItemFilterOptions();

            foreach (var gradeToggle in gradeToggles)
                itemFilterOptionType.Grade |= gradeToggle.toggle.isOn ? gradeToggle.grade : Grade.All;

            foreach (var elementalToggle in elementalToggles)
                itemFilterOptionType.Elemental |= elementalToggle.toggle.isOn ? elementalToggle.elemental : Elemental.All;

            foreach (var itemTypeToggle in itemTypeToggles)
                itemFilterOptionType.ItemType |= itemTypeToggle.toggle.isOn ? itemTypeToggle.itemType : ItemType.All;

            foreach (var upgradeLevelToggle in upgradeLevelToggles)
                itemFilterOptionType.UpgradeLevel |= upgradeLevelToggle.toggle.isOn ? upgradeLevelToggle.upgradeLevel : UpgradeLevel.All;

            foreach (var optionCountToggle in optionCountToggles)
                itemFilterOptionType.OptionCount |= optionCountToggle.toggle.isOn ? optionCountToggle.optionCount : OptionCount.All;

            foreach (var withSkillToggle in withSkillToggles)
                itemFilterOptionType.WithSkill |= withSkillToggle.toggle.isOn ? withSkillToggle.withSkill : WithSkill.All;

            itemFilterOptionType.SearchText = _searchInputField.text;

            _itemFilterOptions = itemFilterOptionType;
        }

        protected ItemFilterOptions GetItemFilterOptionType()
        {
            return _itemFilterOptions;
        }

        protected void ResetViewFromFilterOption()
        {
            SetTogglesFromFilterOption();
            SetInputFiledFromFilterOption();
        }

        private void SetTogglesFromFilterOption()
        {
            if (_itemFilterOptions.Grade != Grade.All)
                foreach (var gradeToggle in gradeToggles)
                    gradeToggle.toggle.isOn = _itemFilterOptions.Grade.HasFlag(gradeToggle.grade);
            else
                ResetToAll(gradeToggles);

            if (_itemFilterOptions.Elemental != Elemental.All)
                foreach (var elementalToggle in elementalToggles)
                    elementalToggle.toggle.isOn = _itemFilterOptions.Elemental.HasFlag(elementalToggle.elemental);
            else
                ResetToAll(elementalToggles);

            if (_itemFilterOptions.ItemType != ItemType.All)
                foreach (var itemTypeToggle in itemTypeToggles)
                    itemTypeToggle.toggle.isOn = _itemFilterOptions.ItemType.HasFlag(itemTypeToggle.itemType);
            else
                ResetToAll(itemTypeToggles);

            if (_itemFilterOptions.UpgradeLevel != UpgradeLevel.All)
                foreach (var upgradeLevelToggle in upgradeLevelToggles)
                    upgradeLevelToggle.toggle.isOn = _itemFilterOptions.UpgradeLevel.HasFlag(upgradeLevelToggle.upgradeLevel);
            else
                ResetToAll(upgradeLevelToggles);

            if (_itemFilterOptions.OptionCount != OptionCount.All)
                foreach (var optionCountToggle in optionCountToggles)
                    optionCountToggle.toggle.isOn = _itemFilterOptions.OptionCount.HasFlag(optionCountToggle.optionCount);
            else
                ResetToAll(optionCountToggles);

            if (_itemFilterOptions.WithSkill != WithSkill.All)
                foreach (var withSkillToggle in withSkillToggles)
                    withSkillToggle.toggle.isOn = _itemFilterOptions.WithSkill.HasFlag(withSkillToggle.withSkill);
            else
                ResetToAll(withSkillToggles);
        }

        private void SetInputFiledFromFilterOption()
        {
            _searchInputField.text = _itemFilterOptions.SearchText;
        }

        public static ItemType ItemSubTypeToItemType(ItemSubType itemSubType)
        {
            switch (itemSubType)
            {
                case ItemSubType.Weapon:
                    return ItemType.Weapon;
                case ItemSubType.Armor:
                    return ItemType.Armor;
                case ItemSubType.Belt:
                    return ItemType.Belt;
                case ItemSubType.Necklace:
                    return ItemType.Necklace;
                case ItemSubType.Ring:
                    return ItemType.Ring;
                case ItemSubType.Aura:
                    return ItemType.Aura;
                default:
                    return ItemType.All;
            }
        }
    }
}
