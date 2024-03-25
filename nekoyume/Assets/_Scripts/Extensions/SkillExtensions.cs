using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Nekoyume.Game;
using Nekoyume.L10n;
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
                sb.AppendLine($"{L10nManager.Localize("UI_SKILL_EFFECT")}: {buffSkill.EffectToString()}");
            }
            else
            {
                sb.AppendLine($"{L10nManager.Localize("UI_SKILL_POWER")}: {skill.Power}");
            }
            sb.Append($"{L10nManager.Localize("UI_SKILL_CHANCE")}: {skill.Chance}%");

            return sb.ToString();
        }

        public static string EffectToString(this Skill skill)
        {
            var row = skill.SkillRow;
            return EffectToString(
                row.Id,
                row.SkillType,
                skill.Power,
                skill.StatPowerRatio,
                skill.ReferencedStatType);
        }

        public static string EffectToString(SkillSheet.Row skillRow, EquipmentItemOptionSheet.Row optionRow, bool max)
        {
            return EffectToString(
                skillRow.Id,
                skillRow.SkillType,
                max ? optionRow.SkillDamageMax : optionRow.SkillChanceMin,
                max ? optionRow.StatDamageRatioMax : optionRow.StatDamageRatioMin,
                optionRow.ReferencedStatType);
        }

        public static string EffectToString(
            int skillId,
            SkillType skillType,
            long power,
            int statPowerRatio,
            StatType referencedStatType)
        {
            var sheets = TableSheets.Instance;

            if(sheets.SkillSheet.TryGetValue(skillId,out var skillSheetRow))
            {
                switch (skillSheetRow.SkillCategory)
                {
                    case SkillCategory.ShatterStrike:
                        var percentageFormat = new NumberFormatInfo { PercentPositivePattern = 1, PercentNegativePattern = 1 };
                        var multiplierText = (statPowerRatio / 10000m).ToString("P2", percentageFormat);
                        return $"({multiplierText} {referencedStatType})";
                    case SkillCategory.Focus:
                    case SkillCategory.Dispel:
                        if(sheets.SkillActionBuffSheet.TryGetValue(skillId, out var skillActionBuffSheetRow) &&
                            sheets.ActionBuffSheet.TryGetValue(skillActionBuffSheetRow.BuffIds.First(),out var actionBuffSheetRow))
                        {
                            return $"{actionBuffSheetRow.Chance}%";
                        }
                        break;
                    default:
                        break;
                }
            }

            var isBuff = sheets.SkillBuffSheet.TryGetValue(skillId, out var skillBuffRow) &&
                (skillType == SkillType.Buff || skillType == SkillType.Debuff);
            var showPercent = false;
            if (isBuff)
            {
                var firstBuffId = skillBuffRow.BuffIds.First();
                var buffRow = sheets.StatBuffSheet[firstBuffId];
                power += buffRow.Value;
                showPercent = buffRow.OperationType == StatModifier.OperationType.Percentage;
            }

            var valueText = power.ToString();
            if (statPowerRatio > 0)
            {
                if (referencedStatType != StatType.NONE)
                {
                    var percentageFormat = new NumberFormatInfo { PercentPositivePattern = 1, PercentNegativePattern = 1 };
                    if (power != 0)
                    {
                        var sign = statPowerRatio >= 0 ? "+" : "-";
                        var multiplierText = (Math.Abs(statPowerRatio) / 10000m).ToString("P2", percentageFormat);
                        valueText = $"({valueText} {sign} {multiplierText} {referencedStatType})";
                    }
                    else
                    {
                        var multiplierText = (statPowerRatio / 10000m).ToString("P2", percentageFormat);
                        valueText = $"({multiplierText} {referencedStatType})";
                    }
                }
            }

            return valueText + (showPercent ? "%" : string.Empty);
        }
    }
}
