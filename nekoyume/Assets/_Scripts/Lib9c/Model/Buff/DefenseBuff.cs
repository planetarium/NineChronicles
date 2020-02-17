using System;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class DefenseBuff : Buff
    {
        public DefenseBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
