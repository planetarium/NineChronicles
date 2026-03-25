using System;
using System.Collections.Generic;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public struct ItemFilterOptions
    {
        public ItemFilterPopupBase.GradeFilterOption Grade;
        public ItemFilterPopupBase.Elemental Elemental;
        public ItemFilterPopupBase.ItemType ItemType;
        public ItemFilterPopupBase.UpgradeLevel UpgradeLevel;
        public ItemFilterPopupBase.OptionCount OptionCount;
        public ItemFilterPopupBase.WithSkill WithSkill;

        public string SearchText;

        public bool IsNeedFilter =>
            Grade != ItemFilterPopupBase.GradeFilterOption.All ||
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
        /// 등급 필터 옵션(UI 토글/프리팹과 분리된, 실제 필터링에 사용되는 타입)
        /// </summary>
        [Flags]
        public enum GradeFilterOption
        {
            All = 0,
            BelowEpic = 1 << 0, // Normal + Rare
            Epic = 1 << 1,
            Unique = 1 << 2,
            Legendary = 1 << 3,
            Divinity = 1 << 4,
            Mythic = 1 << 5,
            Transcendent = 1 << 6,
        }

        private const string BelowEpicGradeKey = "UI_GRADE_BELOW_EPIC";

        [Flags]
        public enum Elemental
        {
            All = 0,
            Normal = 1 << 0,
            Fire = 1 << 1,
            Water = 1 << 2,
            Land = 1 << 3,
            Wind = 1 << 4
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
            Grimoire = 1 << 6
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
            Level5 = 1 << 5,
            Level6More = 1 << 6
        }

        [Flags]
        public enum OptionCount
        {
            All = 0,
            One = 1 << 0,
            Two = 1 << 1,
            Three = 1 << 2
        }

        [Flags]
        public enum WithSkill
        {
            All = 0,
            None = 1 << 0,
            With = 1 << 1
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
                {
                    toggle.isOn = IsAll;
                }
            }

            public void OffAllToggle()
            {
                if (IsAll && toggle.isOn)
                {
                    toggle.isOn = false;
                }
            }
        }

        [Serializable]
        private class GradeToggle : ItemToggleType
        {
            public GradeFilterOption option;

            public override bool IsAll => option == GradeFilterOption.All;
            public override string GetOptionName =>
                option == GradeFilterOption.BelowEpic
                    ? L10nManager.Localize(BelowEpicGradeKey)
                    : option.ToString();
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
            public override string GetOptionName => upgradeLevel switch
            {
                UpgradeLevel.All => "All",
                UpgradeLevel.Level0 => "+0",
                UpgradeLevel.Level1 => "+1",
                UpgradeLevel.Level2 => "+2",
                UpgradeLevel.Level3 => "+3",
                UpgradeLevel.Level4 => "+4",
                UpgradeLevel.Level5 => "+5",
                UpgradeLevel.Level6More => "+6 more",
                _ => upgradeLevel.ToString()
            };
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

            // 신규 등급이 enum에 추가되었는데 프리팹 토글이 누락된 경우(예: Transcendent),
            // 런타임에서 최소한의 보정(토글 복제)으로 크래시를 방지합니다.
            EnsureGradeToggles();
            ValidateGradeTogglesOrThrow();
            InitializeToggleGroup();

            CloseWidget = () =>
            {
                if (_searchInputField.isFocused)
                {
                    return;
                }

                Close(true);
            };

            _deselectAllButton.onClick.AddListener(DeselectAll);
            _okButton.onClick.AddListener(OnClickOkButton);
        }

#endregion Popup

        /// <summary>
        /// grade 토글 목록이 enum 정의와 일치하는지 검증합니다.
        /// 누락되면 자동 보정하지 않고 즉시 예외를 던져(빠르게 발견) 프리팹 수정으로 해결하도록 합니다.
        /// </summary>
        private void ValidateGradeTogglesOrThrow()
        {
            if (gradeToggles is null || gradeToggles.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name}: gradeToggles is null or empty. " +
                    "Please update the prefab to include grade toggles for all grades.");
            }

            foreach (var t in gradeToggles)
            {
                if (t is null || t.toggle == null)
                {
                    throw new InvalidOperationException(
                        $"{GetType().Name}: gradeToggles contains a null Toggle reference. " +
                        "Please fix the prefab toggle bindings.");
                }
            }

            // Flags enum이지만, 현재는 power-of-two 값들만 정의되어 있으므로 정의된 값들을 모두 요구한다.
            var definedOptions = (GradeFilterOption[])Enum.GetValues(typeof(GradeFilterOption));
            foreach (var opt in definedOptions)
            {
                if (opt == GradeFilterOption.All)
                {
                    continue;
                }

                if (!gradeToggles.Exists(x => x.option == opt))
                {
                    throw new InvalidOperationException(
                        $"{GetType().Name}: missing grade toggle for '{opt}'. " +
                        "Please update the prefab to include this grade toggle.");
                }
            }
        }

        private void EnsureGradeToggles()
        {
            if (gradeToggles is null || gradeToggles.Count == 0)
            {
                return;
            }

            // Template: 가장 높은 등급 토글(=보통 마지막)을 복제해 새 토글을 만든다.
            GradeToggle template = null;
            foreach (var t in gradeToggles)
            {
                if (t is null || t.toggle == null) continue;
                if (t.option == GradeFilterOption.All) continue;
                if (template == null || (int)t.option > (int)template.option) template = t;
            }

            if (template == null || template.toggle == null)
            {
                return;
            }

            var definedOptions = (GradeFilterOption[])Enum.GetValues(typeof(GradeFilterOption));
            var createdCount = 0;
            foreach (var opt in definedOptions)
            {
                if (opt == GradeFilterOption.All) continue;
                if (gradeToggles.Exists(x => x != null && x.option == opt)) continue;

                // Clone template toggle GameObject under same parent so layout works.
                var clonedGo = Instantiate(template.toggle.gameObject, template.toggle.transform.parent);
                clonedGo.name = $"{template.toggle.gameObject.name}_{opt}";
                var clonedToggle = clonedGo.GetComponent<Toggle>();
                if (clonedToggle == null)
                {
                    Destroy(clonedGo);
                    continue;
                }

                // Ensure it's off by default (BindToggleEvent에서 All 토글 로직이 다시 정리함).
                clonedToggle.isOn = false;

                gradeToggles.Add(new GradeToggle
                {
                    toggle = clonedToggle,
                    option = opt,
                });
                createdCount++;
            }
        }

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
                var optionName = item.GetOptionName;
                item.toggle.name = optionName;

                // 일부 프리팹은 UGUI Text 대신 TMP를 사용합니다.
                // 템플릿 토글을 복제해서 누락 등급을 자동 생성할 때(예: Transcendent),
                // TMP 라벨을 갱신하지 않으면 텍스트가 그대로 복제되어 "Mythic이 2개"처럼 보일 수 있습니다.
                var uguiText = item.toggle.GetComponentInChildren<Text>(true);
                if (uguiText != null)
                {
                    uguiText.text = optionName;
                }

                var tmpText = item.toggle.GetComponentInChildren<TMP_Text>(true);
                if (tmpText != null)
                {
                    tmpText.text = optionName;
                }

                if (item.IsAll)
                {
                    item.toggle.onValueChanged.AddListener(isOn =>
                    {
                        if (isOn)
                        {
                            ResetToAll(toggles);
                        }
                        else if (IsOffAllToggle(toggles))
                        {
                            ResetToAll(toggles);
                        }
                    });
                }
                else
                {
                    item.toggle.onValueChanged.AddListener(isOn =>
                    {
                        if (isOn)
                        {
                            OffAllToggle(toggles);
                        }
                        else if (IsOffAllToggle(toggles))
                        {
                            ResetToAll(toggles);
                        }
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
                {
                    return false;
                }
            }

            return true;
        }

        private void OffAllToggle<T>(List<T> toggles) where T : ItemToggleType
        {
            foreach (var item in toggles)
            {
                item.OffAllToggle();
            }
        }

        private void ResetToAll<T>(List<T> toggles) where T : ItemToggleType
        {
            foreach (var item in toggles)
            {
                item.ResetToAll();
            }
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
            {
                if (gradeToggle.toggle.isOn && gradeToggle.option != GradeFilterOption.All)
                {
                    itemFilterOptionType.Grade |= gradeToggle.option;
                }
            }

            foreach (var elementalToggle in elementalToggles)
            {
                itemFilterOptionType.Elemental |= elementalToggle.toggle.isOn ? elementalToggle.elemental : Elemental.All;
            }

            foreach (var itemTypeToggle in itemTypeToggles)
            {
                itemFilterOptionType.ItemType |= itemTypeToggle.toggle.isOn ? itemTypeToggle.itemType : ItemType.All;
            }

            foreach (var upgradeLevelToggle in upgradeLevelToggles)
            {
                itemFilterOptionType.UpgradeLevel |= upgradeLevelToggle.toggle.isOn ? upgradeLevelToggle.upgradeLevel : UpgradeLevel.All;
            }

            foreach (var optionCountToggle in optionCountToggles)
            {
                itemFilterOptionType.OptionCount |= optionCountToggle.toggle.isOn ? optionCountToggle.optionCount : OptionCount.All;
            }

            foreach (var withSkillToggle in withSkillToggles)
            {
                itemFilterOptionType.WithSkill |= withSkillToggle.toggle.isOn ? withSkillToggle.withSkill : WithSkill.All;
            }

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
            if (_itemFilterOptions.Grade != GradeFilterOption.All)
            {
                foreach (var gradeToggle in gradeToggles)
                {
                    gradeToggle.toggle.isOn =
                        gradeToggle.option != GradeFilterOption.All &&
                        _itemFilterOptions.Grade.HasFlag(gradeToggle.option);
                }
            }
            else
            {
                ResetToAll(gradeToggles);
            }

            if (_itemFilterOptions.Elemental != Elemental.All)
            {
                foreach (var elementalToggle in elementalToggles)
                {
                    elementalToggle.toggle.isOn = _itemFilterOptions.Elemental.HasFlag(elementalToggle.elemental);
                }
            }
            else
            {
                ResetToAll(elementalToggles);
            }

            if (_itemFilterOptions.ItemType != ItemType.All)
            {
                foreach (var itemTypeToggle in itemTypeToggles)
                {
                    itemTypeToggle.toggle.isOn = _itemFilterOptions.ItemType.HasFlag(itemTypeToggle.itemType);
                }
            }
            else
            {
                ResetToAll(itemTypeToggles);
            }

            if (_itemFilterOptions.UpgradeLevel != UpgradeLevel.All)
            {
                foreach (var upgradeLevelToggle in upgradeLevelToggles)
                {
                    upgradeLevelToggle.toggle.isOn = _itemFilterOptions.UpgradeLevel.HasFlag(upgradeLevelToggle.upgradeLevel);
                }
            }
            else
            {
                ResetToAll(upgradeLevelToggles);
            }

            if (_itemFilterOptions.OptionCount != OptionCount.All)
            {
                foreach (var optionCountToggle in optionCountToggles)
                {
                    optionCountToggle.toggle.isOn = _itemFilterOptions.OptionCount.HasFlag(optionCountToggle.optionCount);
                }
            }
            else
            {
                ResetToAll(optionCountToggles);
            }

            if (_itemFilterOptions.WithSkill != WithSkill.All)
            {
                foreach (var withSkillToggle in withSkillToggles)
                {
                    withSkillToggle.toggle.isOn = _itemFilterOptions.WithSkill.HasFlag(withSkillToggle.withSkill);
                }
            }
            else
            {
                ResetToAll(withSkillToggles);
            }
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
                case ItemSubType.Grimoire:
                    return ItemType.Grimoire;
                default:
                    return ItemType.All;
            }
        }

        public static WithSkill ItemSubTypeToWithSkill(bool skillContains)
        {
            return skillContains ? WithSkill.With : WithSkill.None;
        }
    }
}
