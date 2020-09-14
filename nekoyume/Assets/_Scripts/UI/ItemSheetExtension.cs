using Nekoyume.L10n;
using Nekoyume.TableData;

namespace Nekoyume.UI
{
    public static class ItemSheetExtension
    {
        public static string GetLocalizedName(this ItemSheet.Row value)
        {
            return L10nManager.Localize($"ITEM_NAME_{value.Id}");
        }
    }
}
