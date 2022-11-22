using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class DamageReductionBuff : StatBuff
    {
        public DamageReductionBuff(StatBuffSheet.Row row) : base(row)
        {
        }
    }
}
