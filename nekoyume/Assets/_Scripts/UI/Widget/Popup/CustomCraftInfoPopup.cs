using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Game;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI
{
    using UniRx;
    public class CustomCraftInfoPopup : PopupWidget
    {
        [SerializeField]
        private CustomCraftStatScroll statScroll;

        [SerializeField]
        private CustomCraftSkillScroll skillScroll;

        [SerializeField]
        private CategoryTabButton statTabButton;

        [SerializeField]
        private CategoryTabButton skillTabButton;

        private ItemSubType _selectedSubtype;
        private CategoryTabButton _selectedTabButton;

        public override void Initialize()
        {
            base.Initialize();
            statTabButton.OnClick.Subscribe(ShowStatView).AddTo(gameObject);
            skillTabButton.OnClick.Subscribe(ShowSkillView).AddTo(gameObject);
        }

        public void Show(ItemSubType subType, bool ignoreShowAnimation = false)
        {
            _selectedSubtype = subType;
            _selectedTabButton = statTabButton;
            ShowStatView(_selectedTabButton);
            base.Show(ignoreShowAnimation);
        }

        private void ShowStatView(CategoryTabButton tabButton)
        {
            if (tabButton != _selectedTabButton && _selectedTabButton != null)
            {
                _selectedTabButton.SetToggledOff();
            }

            tabButton.SetToggledOn();
            _selectedTabButton = tabButton;

            var relationshipRow = TableSheets.Instance.CustomEquipmentCraftRelationshipSheet
                .OrderedList
                .First(row => row.Relationship >= ReactiveAvatarState.Relationship);
            var maxCp = relationshipRow.MaxCp;

            var models = TableSheets.Instance.CustomEquipmentCraftOptionSheet.Values
                .Where(row => row.ItemSubType == _selectedSubtype)
                .Select(row =>
                {
                    var compositionString = row.SubStatData
                        .Select(stat => $"{stat.StatType} {stat.Ratio}%")
                        .Aggregate((str1, str2) => $"{str1} / {str2}");
                    var totalCpString = row.SubStatData
                        .Select(stat =>
                            $"{stat.StatType} {(int) CPHelper.ConvertCpToStat(stat.StatType, maxCp / 100m * stat.Ratio, 1)}")
                        .Aggregate((str1, str2) => $"{str1} / {str2}");
                    return new CustomCraftStatCell.Model
                        {CompositionString = compositionString, SubStatTotalString = totalCpString};
                }).ToList();

            statScroll.UpdateData(models);
            statScroll.gameObject.SetActive(true);
            skillScroll.gameObject.SetActive(false);
        }

        private void ShowSkillView(CategoryTabButton tabButton)
        {
            if (tabButton != _selectedTabButton && _selectedTabButton != null)
            {
                _selectedTabButton.SetToggledOff();
            }

            tabButton.SetToggledOn();
            _selectedTabButton = tabButton;

            var skillRows = TableSheets.Instance.CustomEquipmentCraftRecipeSkillSheet.Values
                .Where(row => row.ItemSubType == _selectedSubtype)
                .Select(row => (TableSheets.Instance.EquipmentItemOptionSheet[row.ItemOptionId], row.Ratio));
            skillScroll.UpdateData(skillRows.Select(tuple => new CustomCraftSkillCell.Model
            {
                SkillName = L10nManager.Localize($"SKILL_NAME_{tuple.Item1.SkillId}"),
                SkillRatio = $"{tuple.Ratio}%",
                OptionRow = tuple.Item1,
                SkillRow = TableSheets.Instance.SkillSheet[tuple.Item1.SkillId]
            }));
            skillScroll.gameObject.SetActive(true);
            statScroll.gameObject.SetActive(false);
        }
    }
}
