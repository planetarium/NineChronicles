using System;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    [Serializable]
    public class HPBuff : Buff
    {
        public HPBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
