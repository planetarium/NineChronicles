using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class DodgeBuff : Buff
    {
        public DodgeBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
