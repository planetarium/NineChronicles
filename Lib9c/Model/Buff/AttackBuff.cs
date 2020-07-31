using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class AttackBuff : Buff
    {
        public AttackBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
