using System;
using Nekoyume.EnumType;
using Nekoyume.TableData;

namespace Nekoyume.Game.Buff
{
    public static class BuffFactory
    {
        public static Buff Get(BuffSheet.Row row)
        {
            switch (row.category)
            {
                case BuffCategory.Attack:
                    return new AttackBuff(row);
                case BuffCategory.Defense:
                    return new DefenseBuff(row);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
