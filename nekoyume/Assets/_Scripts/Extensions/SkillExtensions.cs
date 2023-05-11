using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nekoyume.L10n;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume
{
    public static class SkillExtensions
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

        public static string GetLocalizedDescription(this Skill skill)
        {
            var sb = new StringBuilder();
            if (skill is BuffSkill buffSkill)
            {
                var sheets = Game.Game.instance.TableSheets;
                var stat = Game.Game.instance.Stage.GetPlayer().Model.Stats;
                var buffs = BuffFactory.GetBuffs(
                    stat,
                    skill,
                    sheets.SkillBuffSheet,
                    sheets.StatBuffSheet,
                    sheets.SkillActionBuffSheet,
                    sheets.ActionBuffSheet).OfType<StatBuff>();
                if (buffs.Any())
                {
                    var buff = buffs.First();
                    var powerValue = buff.RowData.EffectToString(skill.Power);
                    sb.AppendLine($"{L10nManager.Localize("UI_SKILL_EFFECT")}: {powerValue}");
                }
            }
            else
            {
                sb.AppendLine($"{L10nManager.Localize("UI_SKILL_POWER")}: {skill.Power}");
            }
            sb.Append($"{L10nManager.Localize("UI_SKILL_CHANCE")}: {skill.Chance}%");

            return sb.ToString();
        }
    }
}
