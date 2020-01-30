using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Helper;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.UI
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

            return LocalizationManager.Localize($"SKILL_NAME_{row.Id}");
        }

        public static Sprite GetIcon(this SkillSheet.Row row)
        {
            return SpriteHelper.GetSkillIcon(row.Id);
        }

        // 매번 결정적인 결과를 리턴하기에 캐싱함.
        public static List<BuffSheet.Row> GetBuffs(this SkillSheet.Row row)
        {
            if (SkillBuffs.ContainsKey(row.Id))
                return SkillBuffs[row.Id];

            var buffs = new List<BuffSheet.Row>();
            SkillBuffs[row.Id] = buffs;

            var skillBuffSheet = Game.Game.instance.TableSheets.SkillBuffSheet;
            if (!skillBuffSheet.TryGetValue(row.Id, out var skillBuffRow))
                return buffs;

            var buffSheet = Game.Game.instance.TableSheets.BuffSheet;
            foreach (var buffId in skillBuffRow.BuffIds)
            {
                if (!buffSheet.TryGetValue(buffId, out var buffRow))
                    continue;

                buffs.Add(buffRow);
            }

            return buffs;
        }
    }
}
