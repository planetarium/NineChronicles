using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.TableData.CustomEquipmentCraft;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI
{
    public class CustomCraftInfoPopup : PopupWidget
    {
        [SerializeField]
        private CustomCraftStatScroll statScroll;

        public void Show(ItemSubType subType, bool ignoreShowAnimation = false)
        {
            var optionRows = TableSheets.Instance.CustomEquipmentCraftOptionSheet.Values
                .Where(row => row.ItemSubType == subType);
            var models = new List<CustomCraftStatCell.Model>();
            foreach (var row in optionRows)
            {
                var mergedList = new List<CustomEquipmentCraftOptionSheet.SubStat>();
                foreach (var subStatData in row.SubStatData)
                {
                    var index = mergedList.FindIndex(subStat => subStat.StatType == subStatData.StatType);
                    if (index != -1)
                    {
                        var subStat = mergedList[index];
                        subStat.Ratio += subStatData.Ratio;
                        mergedList[index] = subStat;
                    }
                    else
                    {
                        mergedList.Add(subStatData);
                    }
                }

                var compositionString = mergedList.Select(stat => $"{stat.StatType} {stat.Ratio}%").Aggregate(
                    (str1, str2) => $"{str1} / {str2}");
                models.Add(new CustomCraftStatCell.Model {CompositionString = compositionString});
            }

            statScroll.UpdateData(models);
            base.Show(ignoreShowAnimation);
        }
    }
}
