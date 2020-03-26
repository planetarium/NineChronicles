using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class HitBuff : Buff
    {
        public HitBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
