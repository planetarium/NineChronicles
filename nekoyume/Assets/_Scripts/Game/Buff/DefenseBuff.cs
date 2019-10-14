using System;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    [Serializable]
    public class DefenseBuff : Buff
    {
        public DefenseBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
