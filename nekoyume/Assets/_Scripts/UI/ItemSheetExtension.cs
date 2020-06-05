using Assets.SimpleLocalization;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace Nekoyume.UI
{
    public static class ItemSheetExtension
    {
        public static string GetLocalizedName(this ItemSheet.Row value)
        {
            return LocalizationManager.Localize($"ITEM_NAME_{value.Id}");
        }
    }
}
