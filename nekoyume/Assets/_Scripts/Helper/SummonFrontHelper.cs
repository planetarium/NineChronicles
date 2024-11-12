using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.TableData.Summon;

namespace Nekoyume.Helper
{
    public enum SummonResult
    {
        Aura,
        Grimoire,
        Rune,
        FullCostume,
        Title,
    }

    public static class SummonFrontHelper
    {
        private static readonly Dictionary<SummonResult, List<SummonSheet.Row>> RowListDict = new();
        public static List<SummonSheet.Row> GetSummonRowsBySummonResult(SummonResult summonResult)
        {
            if (RowListDict.TryGetValue(summonResult, out var list))
            {
                return list;
            }

            var sheets = TableSheets.Instance;
            var rows = (summonResult switch
            {
                SummonResult.Aura => sheets.EquipmentSummonSheet.Values.Where(row =>
                    row.Recipes.Any(pair =>
                        sheets.EquipmentItemRecipeSheet.TryGetValue(pair.Item1,
                            out var recipeRow) && recipeRow.ItemSubType == ItemSubType.Aura)),
                SummonResult.Grimoire => sheets.EquipmentSummonSheet.Values.Where(row =>
                    row.Recipes.Any(pair =>
                        sheets.EquipmentItemRecipeSheet.TryGetValue(pair.Item1,
                            out var recipeRow) && recipeRow.ItemSubType == ItemSubType.Grimoire)),
                SummonResult.Rune => sheets.RuneSummonSheet.Values,
                SummonResult.FullCostume => sheets.CostumeSummonSheet.Values.Where(row =>
                    row.Recipes.Any(pair =>
                        sheets.CostumeItemSheet.TryGetValue(pair.Item1, out var costumeRow) &&
                        costumeRow.ItemSubType == ItemSubType.FullCostume)),
                SummonResult.Title => sheets.CostumeSummonSheet.Values.Where(row =>
                    row.Recipes.Any(pair =>
                        sheets.CostumeItemSheet.TryGetValue(pair.Item1, out var costumeRow) &&
                        costumeRow.ItemSubType == ItemSubType.Title)),
                _ => throw new ArgumentOutOfRangeException(nameof(summonResult), summonResult, null)
            }).ToList();
            RowListDict.Add(summonResult, rows);
            return rows;
        }

        public static SummonResult GetSummonResultByRow(SummonSheet.Row row)
        {
            foreach (SummonResult value in Enum.GetValues(typeof(SummonResult)))
            {
                GetSummonRowsBySummonResult(value);
            }

            return RowListDict.FirstOrDefault(pair => pair.Value.Contains(row)).Key;
        }
    }
}
