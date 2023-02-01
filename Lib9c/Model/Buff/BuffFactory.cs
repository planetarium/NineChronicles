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
            switch (row.StatModifier.StatType)
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
            switch (row.StatModifier.StatType)
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

        public static ActionBuff GetActionBuff(int power, ActionBuffSheet.Row row)
        {
            switch (row.ActionBuffType)
            {
                case ActionBuffType.Bleed:
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
            int power,
            ISkill skill,
            SkillBuffSheet skillBuffSheet,
            StatBuffSheet statBuffSheet,
            SkillActionBuffSheet skillActionBuffSheet,
            ActionBuffSheet actionBuffSheet)
        {
            var buffs = new List<Buff>();

            if (skillBuffSheet.TryGetValue(skill.SkillRow.Id, out var skillStatBuffRow))
            {
                foreach (var buffId in skillStatBuffRow.BuffIds)
                {
                    if (!statBuffSheet.TryGetValue(buffId, out var buffRow))
                        continue;

                    if (!skill.CustomField.HasValue)
                    {
                        buffs.Add(GetStatBuff(buffRow));
                    }
                    else
                    {
                        buffs.Add(GetCustomStatBuff(buffRow, skill.CustomField.Value));
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
                        buffs.Add(GetActionBuff(power, buffRow));
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
