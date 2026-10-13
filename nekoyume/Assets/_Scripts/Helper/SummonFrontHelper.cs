using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
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

        /// <summary>
        /// 이 소환 행에 <paramref name="blockIndex"/> 에서 걸리는 보장 개수 부스트. 액션이 쓰는 것과 같은 행이다
        /// (<c>AuraSummon/RuneSummon/CostumeSummon.Execute</c> 의 <c>FindActive</c>).
        /// </summary>
        /// <remarks>
        /// 그룹 id 만으로는 시트를 구분할 수 없어(target 이 시트별) 행이 어느 소환 시트의 것인지로 target 을 고른다.
        /// </remarks>
        public static BoostScheduleSheet.Row FindGuaranteeBoost(SummonSheet.Row row, long blockIndex) =>
            FindGuaranteeBoost(row, blockIndex, Game.Game.instance.TableSheets.BoostScheduleSheet);

        /// <param name="row">소환 행.</param>
        /// <param name="blockIndex">찾을 블록.</param>
        /// <param name="boostScheduleSheet">
        /// 찾을 시트. 결과 재현은 액션이 실제로 본 시트(<c>BoostScheduleFrontHelper.GetSheetForAction</c>)를 넘긴다.
        /// </param>
        public static BoostScheduleSheet.Row FindGuaranteeBoost(
            SummonSheet.Row row,
            long blockIndex,
            BoostScheduleSheet boostScheduleSheet)
        {
            var target = GetGuaranteeBoostTarget(Game.Game.instance.TableSheets, row);
            return target is null
                ? null
                : boostScheduleSheet?.FindActive(target, row.GroupId, blockIndex);
        }

        private static string GetGuaranteeBoostTarget(TableSheets tableSheets, SummonSheet.Row row)
        {
            if (Contains(tableSheets.EquipmentSummonSheet, row))
            {
                return BoostScheduleSheet.Targets.EquipmentSummonGuarantee;
            }

            if (Contains(tableSheets.RuneSummonSheet, row))
            {
                return BoostScheduleSheet.Targets.RuneSummonGuarantee;
            }

            if (Contains(tableSheets.CostumeSummonSheet, row))
            {
                return BoostScheduleSheet.Targets.CostumeSummonGuarantee;
            }

            return null;
        }

        private static bool Contains(SummonSheet sheet, SummonSheet.Row row) =>
            sheet is not null && sheet.TryGetValue(row.GroupId, out var found) && ReferenceEquals(found, row);

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
