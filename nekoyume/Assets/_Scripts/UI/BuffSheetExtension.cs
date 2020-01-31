using Assets.SimpleLocalization;
using Nekoyume.Helper;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.UI
{
    public static class BuffSheetRowExtension
    {
        public static string GetLocalizedName(this BuffSheet.Row row)
        {
            return LocalizationManager.Localize($"BUFF_NAME_{row.Id}");
        }

        public static string GetLocalizedDescription(this BuffSheet.Row row)
        {
            return LocalizationManager.Localize($"BUFF_DESCRIPTION_{row.Id}");
        }

        public static Sprite GetIcon(this BuffSheet.Row row)
        {
            return SpriteHelper.GetBuffIcon(row.IconResource);
        }
    }
}
