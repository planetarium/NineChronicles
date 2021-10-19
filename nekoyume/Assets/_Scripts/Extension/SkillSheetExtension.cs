using System.Collections.Generic;
using Nekoyume.L10n;
using Nekoyume.TableData;

namespace Nekoyume
{
    public static class SkillSheetExtension
    {
        private static readonly Dictionary<int, List<BuffSheet.Row>> SkillBuffs =
            new Dictionary<int, List<BuffSheet.Row>>();

        public static string GetLocalizedName(this SkillSheet.Row row)
        {
            if (row is null)
            {
                throw new System.ArgumentNullException(nameof(row));
            }

            return L10nManager.Localize($"SKILL_NAME_{row.Id}");
        }
    }
}
