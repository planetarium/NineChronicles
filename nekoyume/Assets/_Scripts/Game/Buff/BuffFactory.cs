using System;
using Nekoyume.EnumType;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    public static class BuffFactory
    {
        public static Buff Get(BuffSheet.Row row)
        {
            switch (row.StatType)
            {
                case StatType.ATK:
                    return new AttackBuff(row);
                case StatType.DEF:
                    return new DefenseBuff(row);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
