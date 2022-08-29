using System.Collections.Generic;
using Nekoyume.L10n;
using Nekoyume.TableData;

namespace Nekoyume
{
    public static class SkillSheetExtensions
    {
        private static readonly Dictionary<int, List<StatBuffSheet.Row>> SkillBuffs =
            new Dictionary<int, List<StatBuffSheet.Row>>();

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
