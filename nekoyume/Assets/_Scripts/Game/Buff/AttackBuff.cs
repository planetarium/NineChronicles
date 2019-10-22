using System;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    [Serializable]
    public class AttackBuff : Buff
    {
        public AttackBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
