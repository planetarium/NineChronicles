using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Buff;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume
{
    public static class BuffSheetRowExtension
    {
        public static string GetLocalizedName(this Buff buff)
        {
            return L10nManager.Localize($"BUFF_NAME_{buff.BuffInfo.Id}");
        }

        public static string GetLocalizedDescription(this Buff buff)
        {
            var desc = L10nManager.Localize($"BUFF_DESCRIPTION_{buff.BuffInfo.Id}");
            if (buff is StatBuff stat)
            {
                return string.Format(desc, stat.RowData.StatModifier.Value);
            }
            else if (buff is ActionBuff action)
            {
                return desc;
            }
            return $"!{buff.BuffInfo.Id}!";
        }

        public static Sprite GetIcon(this Buff buff)
        {
            return BuffHelper.GetBuffIcon(buff);
        }
    }
}
