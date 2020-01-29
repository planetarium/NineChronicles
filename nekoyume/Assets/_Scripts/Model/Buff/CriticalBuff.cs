using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class CriticalBuff : Buff
    {
        public CriticalBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
