using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using System.Linq;
using Nekoyume.TableData.Event;

namespace Nekoyume.Helper
{
    public static class RecipeHelper
    {
        public static EquipmentItemSheet.Row GetResultEquipmentItemRow(this EquipmentItemRecipeSheet.Row recipeRow)
        {
            return Game.Game.instance.TableSheets
                .EquipmentItemSheet[recipeRow.ResultEquipmentId];
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
