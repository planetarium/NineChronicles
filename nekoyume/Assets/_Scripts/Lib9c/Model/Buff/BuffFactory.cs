using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    public static class BuffFactory
    {
        public static Buff Get(BuffSheet.Row row)
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
                    return new DodgeBuff(row);
                case StatType.SPD:
                    return new SpeedBuff(row);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static IList<Buff> GetBuffs(
            Skill.Skill skill, 
            SkillBuffSheet skillBuffSheet, 
            BuffSheet buffSheet)
        {
            var buffs = new List<Buff>();
            if (!skillBuffSheet.TryGetValue(skill.skillRow.Id, out var skillBuffRow))
                return buffs;
            
            foreach (var buffId in skillBuffRow.BuffIds)
            {
                if (!buffSheet.TryGetValue(buffId, out var buffRow))
                    continue;

                buffs.Add(BuffFactory.Get(buffRow));
            }

            return buffs;
        }
    }
}
