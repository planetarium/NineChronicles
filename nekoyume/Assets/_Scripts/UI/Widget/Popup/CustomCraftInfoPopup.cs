using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI
{
    public class CustomCraftInfoPopup : PopupWidget
    {
        [SerializeField]
        private CustomCraftStatScroll statScroll;

        [SerializeField]
        private CustomCraftSkillScroll skillScroll;

        public void Show(ItemSubType subType, bool ignoreShowAnimation = false)
        {
            var relationshipRow = TableSheets.Instance.CustomEquipmentCraftRelationshipSheet
                .OrderedList
                .First(row => row.Relationship >= ReactiveAvatarState.Relationship);
            var maxCp = relationshipRow.MaxCp;

            var models = TableSheets.Instance.CustomEquipmentCraftOptionSheet.Values
                .Where(row => row.ItemSubType == subType)
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
            base.Show(ignoreShowAnimation);
        }
    }
}
