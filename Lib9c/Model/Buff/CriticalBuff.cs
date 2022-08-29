using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class CriticalBuff : StatBuff
    {
        public CriticalBuff(StatBuffSheet.Row row) : base(row)
        {
        }
    }
}
