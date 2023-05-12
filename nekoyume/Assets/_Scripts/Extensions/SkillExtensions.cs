using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AsyncIO;
using Nekoyume.Game;
using Nekoyume.L10n;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
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
                sb.AppendLine($"{L10nManager.Localize("UI_SKILL_EFFECT")}: {buffSkill.SkillRow.EffectToString(buffSkill.Power)}");
            }
            else
            {
                sb.AppendLine($"{L10nManager.Localize("UI_SKILL_POWER")}: {skill.Power}");
            }
            sb.Append($"{L10nManager.Localize("UI_SKILL_CHANCE")}: {skill.Chance}%");

            return sb.ToString();
        }

        public static string EffectToString(this SkillSheet.Row row, int power)
        {
            var sheets = TableSheets.Instance;
            var isBuff = sheets.SkillBuffSheet.TryGetValue(row.Id, out var skillBuffRow) &&
                row.SkillType == SkillType.Buff;
            var showPercent = false;
            if (isBuff)
            {
                var firstBuffId = skillBuffRow.BuffIds.First();
                var buffRow = sheets.StatBuffSheet[firstBuffId];
                power += buffRow.Value;
                showPercent = buffRow.OperationType == StatModifier.OperationType.Percentage;
            }

            var valueText = power.ToString();

            if (power > 0)
            {
                var sign = power >= 0 ? "+" : "-";
                if (row.ReferencedStatType != StatType.NONE)
                {
                    var multiplierText = (Math.Abs(row.StatPowerRatio) / 10000m).ToString("0.##");
                    valueText = $"({valueText} {sign} {multiplierText} {row.ReferencedStatType})";
                }
            }

            return valueText + (showPercent ? "%" : string.Empty);
        }
    }
}
