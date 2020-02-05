using Assets.SimpleLocalization;
using Nekoyume.TableData;

namespace Nekoyume.UI
{
    public static class ItemSheetExtension
    {
        public static string GetLocalizedName(this ItemSheet.Row value)
        {
            return LocalizationManager.Localize($"ITEM_NAME_{value.Id}");
        }

        public static string GetLocalizedDescription(this ItemSheet.Row value)
        {
            return LocalizationManager.Localize($"ITEM_DESCRIPTION_{value.Id}");
        }
    }
}
