using System;
using Nekoyume.EnumType;
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
                case StatType.DOG:
                    return new DodgeBuff(row);
                case StatType.SPD:
                    return new SpeedBuff(row);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
