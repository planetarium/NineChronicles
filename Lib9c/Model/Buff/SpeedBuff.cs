using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class SpeedBuff : Buff
    {
        public SpeedBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
