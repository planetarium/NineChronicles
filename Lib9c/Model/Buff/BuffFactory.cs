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

        public static IList<StatBuff> GetBuffs(
            ISkill skill,
            SkillBuffSheet skillBuffSheet,
            StatBuffSheet statBuffSheet)
        {
            var buffs = new List<StatBuff>();
            if (!skillBuffSheet.TryGetValue(skill.SkillRow.Id, out var skillStatBuffRow))
                return buffs;

            foreach (var buffId in skillStatBuffRow.BuffIds)
            {
                if (!statBuffSheet.TryGetValue(buffId, out var buffRow))
                    continue;

                buffs.Add(GetStatBuff(buffRow));
            }

            return buffs;
        }

        public static IList<Buff> GetBuffsV2(
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

                    buffs.Add(GetStatBuff(buffRow));
                }
            }

            if (skillActionBuffSheet.TryGetValue(skill.SkillRow.Id, out var skillActionBuffRow))
            {
                foreach (var buffId in skillActionBuffRow.BuffIds)
                {
                    if (!actionBuffSheet.TryGetValue(buffId, out var buffRow))
                        continue;

                    buffs.Add(GetActionBuff(power, buffRow));
                }
            }

            return buffs;
        }
    }
}
