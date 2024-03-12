using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public struct ItemFilterOptions
    {
        public ItemFilterOptionPopup.Grade Grade;
        public ItemFilterOptionPopup.Elemental Elemental;
        public ItemFilterOptionPopup.ItemType ItemType;
        public ItemFilterOptionPopup.UpgradeLevel UpgradeLevel;
        public ItemFilterOptionPopup.OptionCount OptionCount;
        public ItemFilterOptionPopup.WithSkill WithSkill;

        public string SearchText;

        public bool IsNeedFilter => Grade != ItemFilterOptionPopup.Grade.All ||
                                      Elemental != ItemFilterOptionPopup.Elemental.All ||
                                      ItemType != ItemFilterOptionPopup.ItemType.All ||
                                      UpgradeLevel != ItemFilterOptionPopup.UpgradeLevel.All ||
                                      OptionCount != ItemFilterOptionPopup.OptionCount.All ||
                                      WithSkill != ItemFilterOptionPopup.WithSkill.All;
    }

    public class ItemFilterOptionPopup : PopupWidget
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
            Level1 = 1 << 0,
            Level2 = 1 << 1,
            Level3 = 1 << 2,
            Level4 = 1 << 3,
            Level5 = 1 << 4,
            Level6More = 1 << 5,
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
        }

        [Serializable]
        private class ElementalToggle : ItemToggleType
        {
            public Elemental elemental;

            public override bool IsAll => elemental == Elemental.All;
        }

        [Serializable]
        private class ItemTypeToggle : ItemToggleType
        {
            public ItemType itemType;

            public override bool IsAll => itemType == ItemType.All;
        }

        [Serializable]
        private class UpgradeLevelToggle : ItemToggleType
        {
            public UpgradeLevel upgradeLevel;

            public override bool IsAll => upgradeLevel == UpgradeLevel.All;
        }

        [Serializable]
        private class OptionCountToggle : ItemToggleType
        {
            public OptionCount optionCount;

            public override bool IsAll => optionCount == OptionCount.All;
        }

        [Serializable]
        private class WithSkillToggle : ItemToggleType
        {
            public WithSkill withSkill;

            public override bool IsAll => withSkill == WithSkill.All;
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
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);

            // TODO: IItemFilterOptionWidget로 대체
            var collectionWidget = Find<Collection>();
            collectionWidget.SetItemFilterOption(GetItemFilterOptionType());
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
                if (item.IsAll)
                {
                    item.toggle.onValueChanged.AddListener(isOn =>
                    {
                        if (isOn) ResetToAll(toggles);
                    });
                }
                else
                {
                    item.toggle.onValueChanged.AddListener(isOn =>
                    {
                        if (isOn) item.OffAllToggle();
                        else if (item.IsAll) ResetToAll(toggles);
                    });
                }
            }
        }
        private void ResetToAll<T>(List<T> toggles) where T : ItemToggleType
        {
            foreach (var item in toggles)
                item.ResetToAll();
        }

        private ItemFilterOptions GetItemFilterOptionType()
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

            return itemFilterOptionType;
        }
    }
}
