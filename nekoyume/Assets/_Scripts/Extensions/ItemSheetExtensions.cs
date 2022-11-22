using Nekoyume.L10n;
using Nekoyume.TableData;

namespace Nekoyume
{
    public static class ItemSheetExtensions
    {
        public static string GetLocalizedName(
            this ItemSheet.Row value,
            bool hasColor = true,
            bool useElementalIcon = true)
        {
            if (value is EquipmentItemSheet.Row equipmentRow)
            {
                if (hasColor)
                {
                    return equipmentRow.GetLocalizedName(0, useElementalIcon);
                }

                return LocalizationExtensions.GetLocalizedNonColoredName(
                    equipmentRow.ElementalType,
                    equipmentRow.Id,
                    useElementalIcon);
            }

            if (value is ConsumableItemSheet.Row consumableRow)
            {
                return consumableRow.GetLocalizedName(hasColor);
            }

            return LocalizationExtensions.GetLocalizedNonColoredName(
                value.ElementalType, value.Id, useElementalIcon);
        }

        public static string GetLocalizedDescription(this ItemSheet.Row value)
        {
            return L10nManager.Localize($"ITEM_DESCRIPTION_{value.Id}");;
        }
    }
}
