using Nekoyume.L10n;
using Nekoyume.TableData;
using Nekoyume.Helper;

namespace Nekoyume.UI
{
    public static class ItemSheetExtension
    {
        public static string GetLocalizedName(this ItemSheet.Row value, bool hasColor = true, bool useElementalIcon = true)
        {
            if (!hasColor)
            {
                return L10nManager.Localize($"ITEM_NAME_{value.Id}");
            }

            if (value is EquipmentItemSheet.Row equipmentRow)
            {
                return equipmentRow.GetLocalizedName(0, useElementalIcon);
            }

            if (value is ConsumableItemSheet.Row consumableRow)
            {
                return consumableRow.GetLocalizedName();
            }

            return L10nManager.Localize($"ITEM_NAME_{value.Id}");
        }
    }
}
