using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class HPBuff : Buff
    {
        public HPBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
