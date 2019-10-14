using System;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    [Serializable]
    public class SpeedBuff : Buff
    {
        public SpeedBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
