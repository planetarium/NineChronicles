using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.UI;

namespace Nekoyume.Helper
{
    public static class TextHelper
    {
        public static string GetItemNameInCombinationSlot(ItemUsable itemUsable)
        {
            var itemName = itemUsable.GetLocalizedNonColoredName(false);
            switch (itemUsable)
            {
                case Equipment equipment:
                    if (equipment.level > 0)
                    {
                        return string.Format(L10nManager.Localize("UI_COMBINATION_SLOT_UPGRADE"),
                            itemName,
                            equipment.level);
                    }
                    else
                    {
                        return string.Format(L10nManager.Localize("UI_COMBINATION_SLOT_CRAFT"),
                            itemName);
                    }
                default:
                    return string.Format(L10nManager.Localize("UI_COMBINATION_SLOT_CRAFT"),
                        itemName);
            }
        }
    }
}
