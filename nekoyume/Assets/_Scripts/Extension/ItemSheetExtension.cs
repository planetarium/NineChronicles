using Nekoyume.Arena;
using Nekoyume.TableData;
using Nekoyume.UI;

namespace Nekoyume
{
    public static class ItemSheetExtension
    {
        public static string GetLocalizedName(this ItemSheet.Row value, bool hasColor = true, bool useElementalIcon = true)
        {
            if (value is EquipmentItemSheet.Row equipmentRow)
            {
                if (hasColor)
                {
                    return equipmentRow.GetLocalizedName(0, useElementalIcon);
                }
                else
                {
                    return LocalizationExtension.GetLocalizedNonColoredName(
                        equipmentRow.ElementalType,
                        equipmentRow.Id,
                        useElementalIcon);
                }
            }

            if (value is ConsumableItemSheet.Row consumableRow)
            {
                return LocalizationExtension.GetLocalizedName(consumableRow, hasColor);
            }

            return LocalizationExtension.GetLocalizedNonColoredName(
                value.ElementalType, value.Id, useElementalIcon);
        }

        public static int GetIconResourceId(
            this ItemSheet.Row row,
            ArenaSheet arenaSheet) =>
            row.Id.GetIconResourceId(arenaSheet);
        
        public static int GetIconResourceId(
            this int itemSheetRowId,
            ArenaSheet arenaSheet)
        {
            // NOTE: `700_102` is new arena medal item id.
            if (itemSheetRowId < 700_102 ||
                itemSheetRowId >= 800_000)
            {
                return itemSheetRowId;
            }

            return itemSheetRowId
                .ToArenaNumbers(arenaSheet)
                .ToItemIconResourceId();
        }
    }
}
