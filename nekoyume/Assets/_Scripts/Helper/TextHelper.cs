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

        public static string GetWorldNameByStageId(int stageId)
        {
            if (stageId <= 50)
            {
                return L10nManager.Localize("WORLD_NAME_YGGDRASIL");
            }

            if (stageId <= 100)
            {
                return L10nManager.Localize("WORLD_NAME_ALFHEIM");
            }

            if (stageId <= 150)
            {
                return L10nManager.Localize("WORLD_NAME_SVARTALFHEIM");
            }

            if (stageId <= 200)
            {
                return L10nManager.Localize("WORLD_NAME_ASGARD");
            }

            if (stageId <= 300)
            {
                return L10nManager.Localize("WORLD_NAME_MUSPELHEIM");
            }

            if (stageId > 10_000_000)
            {
                return L10nManager.Localize("WORLD_NAME_MIMISBRUNNR");
            }

            return string.Empty;
        }
    }
}
