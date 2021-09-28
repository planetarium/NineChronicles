using Nekoyume.L10n;
using Nekoyume.TableData;

namespace Nekoyume.UI
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
    }
}
