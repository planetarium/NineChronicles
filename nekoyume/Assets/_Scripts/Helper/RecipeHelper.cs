using System.Collections.Generic;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using System.Linq;
using Nekoyume.TableData.Event;

namespace Nekoyume.Helper
{
    public static class RecipeHelper
    {
        private static readonly HashSet<int> MissingResultRecipeIds = new();

        public static EquipmentItemSheet.Row GetResultEquipmentItemRow(this EquipmentItemRecipeSheet.Row recipeRow)
        {
            return Game.Game.instance.TableSheets
                .EquipmentItemSheet[recipeRow.ResultEquipmentId];
        }

        /// <summary>
        /// 결과 장비 행이 시트에 없을 수 있는 곳(테이블 패치로 레시피만 먼저 들어온 경우)에서 쓴다.
        /// 없으면 레시피 하나당 한 번만 경고를 남기고 false — 레시피 목록을 만드는 곳에서 그 레시피를 빼는 데 쓴다.
        /// </summary>
        public static bool TryGetResultEquipmentItemRow(
            this EquipmentItemRecipeSheet.Row recipeRow,
            out EquipmentItemSheet.Row row)
        {
            if (Game.Game.instance.TableSheets.EquipmentItemSheet
                .TryGetValue(recipeRow.ResultEquipmentId, out row))
            {
                return true;
            }

            if (MissingResultRecipeIds.Add(recipeRow.Id))
            {
                NcDebug.LogWarning(
                    $"[RecipeHelper] recipe {recipeRow.Id} result equipment {recipeRow.ResultEquipmentId} is not in EquipmentItemSheet — skipped.");
            }

            return false;
        }

        public static ConsumableItemSheet.Row GetResultConsumableItemRow(this ConsumableItemRecipeSheet.Row recipeRow)
        {
            return Game.Game.instance.TableSheets
                .ConsumableItemSheet[recipeRow.ResultConsumableItemId];
        }

        public static MaterialItemSheet.Row GetResultMaterialItemRow(this EventMaterialItemRecipeSheet.Row recipeRow)
        {
            return Game.Game.instance.TableSheets
                .MaterialItemSheet[recipeRow.ResultMaterialItemId];
        }

        public static DecimalStat GetUniqueStat(this EquipmentItemSheet.Row row)
        {
            return row.Stat ?? new DecimalStat(StatType.NONE);
        }

        public static DecimalStat GetUniqueStat(this ConsumableItemSheet.Row row)
        {
            return row.Stats.Any() ? row.Stats.First() : new DecimalStat(StatType.NONE);
        }
    }
}
