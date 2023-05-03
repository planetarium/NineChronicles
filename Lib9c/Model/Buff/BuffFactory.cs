using System;
using System.Collections.Generic;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    public static class BuffFactory
    {
        public static StatBuff GetStatBuff(StatBuffSheet.Row row)
        {
            switch (row.StatType)
            {
                case StatType.HP:
                    return new HPBuff(row);
                case StatType.ATK:
                    return new AttackBuff(row);
                case StatType.DEF:
                    return new DefenseBuff(row);
                case StatType.CRI:
                    return new CriticalBuff(row);
                case StatType.HIT:
                    return new HitBuff(row);
                case StatType.SPD:
                    return new SpeedBuff(row);
                case StatType.DRR:
                case StatType.DRV:
                    return new DamageReductionBuff(row);
                case StatType.CDMG:
                    return new CriticalDamageBuff(row);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static StatBuff GetCustomStatBuff(StatBuffSheet.Row row, SkillCustomField customField)
        {
            switch (row.StatType)
            {
                case StatType.HP:
                    return new HPBuff(customField, row);
                case StatType.ATK:
                    return new AttackBuff(customField, row);
                case StatType.DEF:
                    return new DefenseBuff(customField, row);
                case StatType.CRI:
                    return new CriticalBuff(customField, row);
                case StatType.HIT:
                    return new HitBuff(customField, row);
                case StatType.SPD:
                    return new SpeedBuff(customField, row);
                case StatType.DRR:
                case StatType.DRV:
                    return new DamageReductionBuff(customField, row);
                case StatType.CDMG:
                    return new CriticalDamageBuff(customField, row);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static ActionBuff GetActionBuff(Stats stat, ActionBuffSheet.Row row)
        {
            switch (row.ActionBuffType)
            {
                case ActionBuffType.Bleed:
                    var power = (int)decimal.Round(stat.ATK * row.ATKPowerRatio);
                    return new Bleed(row, power);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static ActionBuff GetCustomActionBuff(SkillCustomField customField, ActionBuffSheet.Row row)
        {
            switch (row.ActionBuffType)
            {
                case ActionBuffType.Bleed:
                    return new Bleed(customField, row);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static IList<Buff> GetBuffs(
            CharacterStats stats,
            ISkill skill,
            SkillBuffSheet skillBuffSheet,
            StatBuffSheet statBuffSheet,
            SkillActionBuffSheet skillActionBuffSheet,
            ActionBuffSheet actionBuffSheet)
        {
            var buffs = new List<Buff>();
            var extraValueBuff = skill is BuffSkill && skill.Power > 0;

            if (skillBuffSheet.TryGetValue(skill.SkillRow.Id, out var skillStatBuffRow))
            {
                foreach (var buffId in skillStatBuffRow.BuffIds)
                {
                    if (!statBuffSheet.TryGetValue(buffId, out var buffRow))
                        continue;

                    var customField = skill.CustomField;
                    if (!customField.HasValue &&
                        extraValueBuff)
                    {
                        int additionalPower;
                        // If ReferencedStatType exists,
                        // buff value = original value + (referenced stat * (Skill.Power / 10000))
                        if (buffRow.ReferencedStatType != StatType.NONE)
                        {
                            var statMap = stats.StatWithItems;
                            var multiplier = skill.Power / 10000m;
                            additionalPower = (int)Math.Round(
                                statMap.GetStat(buffRow.ReferencedStatType) * multiplier);
                        }
                        // Else, buff value = original value + Skill.Power
                        else
                        {
                            additionalPower = skill.Power;
                        }
                        
                        customField = new SkillCustomField()
                        {
                            BuffDuration = buffRow.Duration,
                            BuffValue = buffRow.Value + additionalPower,
                        };
                    }

                    if (!customField.HasValue)
                    {
                        buffs.Add(GetStatBuff(buffRow));
                    }
                    else
                    {
                        buffs.Add(GetCustomStatBuff(buffRow, customField.Value));
                    }
                }
            }

            if (skillActionBuffSheet.TryGetValue(skill.SkillRow.Id, out var skillActionBuffRow))
            {
                foreach (var buffId in skillActionBuffRow.BuffIds)
                {
                    if (!actionBuffSheet.TryGetValue(buffId, out var buffRow))
                        continue;

                    if (!skill.CustomField.HasValue)
                    {
                        buffs.Add(GetActionBuff(stats, buffRow));
                    }
                    else
                    {
                        buffs.Add(GetCustomActionBuff(skill.CustomField.Value, buffRow));
                    }
                }
            }

            return buffs;
        }
    }
}
