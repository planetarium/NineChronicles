using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class AttackBuff : StatBuff
    {
        public AttackBuff(StatBuffSheet.Row row) : base(row)
        {
        }
    }
}
