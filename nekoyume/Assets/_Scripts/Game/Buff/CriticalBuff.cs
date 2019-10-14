using System;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    [Serializable]
    public class CriticalBuff : Buff
    {
        public CriticalBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
