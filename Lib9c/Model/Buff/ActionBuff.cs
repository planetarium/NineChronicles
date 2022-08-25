using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class ActionBuff : Buff
    {
        public ActionBuffSheet.Row RowData { get; }

        protected ActionBuff(ActionBuffSheet.Row row) : base(row.TargetType, row.Duration)
        {
            RowData = row;
        }

        protected ActionBuff(ActionBuff value) : base(value)
        {
            RowData = value.RowData;
        }

        public override object Clone()
        {
            return new ActionBuff(this);
        }
    }
}
