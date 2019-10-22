using System;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    [Serializable]
    public class DodgeBuff : Buff
    {
        public DodgeBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
