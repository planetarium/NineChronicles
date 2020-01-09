using System.Collections.Generic;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.UI.Model
{
    public class RecipeInfo
    {
        public class MaterialInfo
        {
            public readonly int Id;
            public readonly bool IsEnough;

            public MaterialInfo(int id)
            {
                Id = id;
                IsEnough = States.Instance.CurrentAvatarState.inventory.HasItem(id);
            }
        }

        public readonly ConsumableItemRecipeSheet.Row Row;
        public readonly IReadOnlyList<MaterialInfo> MaterialInfos;
        public readonly string ResultItemName;
        public readonly bool IsLocked;

        public RecipeInfo(ConsumableItemRecipeSheet.Row row)
        {
            Row = row;
            MaterialInfos = row.MaterialItemIds
                .Select(materialItemId => new MaterialInfo(materialItemId))
                .ToList();
            ResultItemName = GetEquipmentName(Row.ResultConsumableItemId);
            IsLocked = false;
        }

        private static string GetEquipmentName(int id)
        {
            if (id == 0)
                return string.Empty;

            return Game.Game.instance.TableSheets.ItemSheet.TryGetValue(id, out var itemRow)
                ? itemRow.GetLocalizedName()
                : string.Empty;
        }
    }
}
