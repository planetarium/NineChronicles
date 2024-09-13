using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action.CustomEquipmentCraft;
using Nekoyume.Battle;
using Nekoyume.Game;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Nekoyume.TableData.CustomEquipmentCraft;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class CustomEquipmentStatView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI outfitNameText;

        [SerializeField]
        private TextMeshProUGUI baseStatText;

        [SerializeField]
        private TextMeshProUGUI expText;

        [SerializeField]
        private TextMeshProUGUI cpText;

        [SerializeField]
        private TextMeshProUGUI requiredBlockText;

        [SerializeField]
        private TextMeshProUGUI requiredLevelText;

        [SerializeField]
        private TextMeshProUGUI maxMainStatText;

        [SerializeField]
        private TextMeshProUGUI maxSubStatTextFirst;

        [SerializeField]
        private TextMeshProUGUI maxSubStatTextSecond;

        public void Set(
            EquipmentItemSheet.Row equipmentRow,
            CustomEquipmentCraftRelationshipSheet.Row relationshipRow,
            CustomEquipmentCraftRecipeSheet.Row customEquipmentCraftRecipeRow,
            int iconId)
        {
            outfitNameText.SetText(iconId != CustomEquipmentCraft.RandomIconId
                ? L10nManager.LocalizeItemName(iconId)
                : L10nManager.Localize("UI_RANDOM_OUTFIT"));
            baseStatText.SetText($"{equipmentRow.Stat.DecimalStatToString()}");
            expText.SetText($"EXP {equipmentRow.Exp?.ToCurrencyNotation()}");
            cpText.SetText($"CP: {relationshipRow.MinCp}-{relationshipRow.MaxCp}");
            maxMainStatText.SetText(
                $"{equipmentRow.Stat.StatType} : MAX {(long)CPHelper.ConvertCpToStat(equipmentRow.Stat.StatType, relationshipRow.MaxCp, 1)}");
            requiredBlockText.SetText($"{customEquipmentCraftRecipeRow.RequiredBlock}");
            requiredLevelText.SetText(
                $"Lv {TableSheets.Instance.ItemRequirementSheet[equipmentRow.Id].Level}");
            var subStatStrings = GetMaxSubStat(equipmentRow.ItemSubType, relationshipRow.MaxCp, equipmentRow.Stat.StatType);
            maxSubStatTextFirst.SetText(subStatStrings.Item1);
            maxSubStatTextSecond.SetText(subStatStrings.Item2);
        }

        private static (string, string) GetMaxSubStat(ItemSubType subType, long maxCp, StatType mainStatType)
        {
            var options = TableSheets.Instance.CustomEquipmentCraftOptionSheet.Values.Where(row =>
                row.ItemSubType == subType).ToList();
            Dictionary<StatType, long> maxStats = new();
            foreach (var stat in options.SelectMany(row => row.SubStatData).Where(row => row.StatType != mainStatType))
            {
                var statFromMaxCp =
                    (long)CPHelper.ConvertCpToStat(stat.StatType, maxCp * (stat.Ratio / 100m), 1);
                if (maxStats.TryGetValue(stat.StatType, out var maxStat))
                {
                    maxStats[stat.StatType] = Math.Max(maxStat, statFromMaxCp);
                }
                else
                {
                    maxStats[stat.StatType] = statFromMaxCp;
                }
            }

            var statStrings = maxStats.Select(stat => $"{stat.Key} : MAX {stat.Value}").ToList();
            return (statStrings.Take(2).Aggregate((str1, str2) => $"{str1} / {str2}"),
                statStrings.Skip(2).Aggregate((str1, str2) => $"{str1} / {str2}"));
        }
    }
}
