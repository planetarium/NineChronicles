using System;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class StatBuff : Buff
    {
        public StatBuffSheet.Row RowData { get; }
        public SkillCustomField? CustomField { get; }

        public StatBuff(StatBuffSheet.Row row) : base(
            new BuffInfo(row.Id, row.GroupId, row.Chance, row.Duration, row.TargetType))
        {
            RowData = row;
        }

        public StatBuff(SkillCustomField customField, StatBuffSheet.Row row) : base(
            new BuffInfo(row.Id, row.GroupId, row.Chance, customField.BuffDuration, row.TargetType))
        {
            RowData = row;
            CustomField = customField;
        }

        protected StatBuff(StatBuff value) : base(value)
        {
            RowData = value.RowData;
            CustomField = value.CustomField;
        }

        public StatModifier GetModifier()
        {
            var value = CustomField.HasValue ?
                CustomField.Value.BuffValue :
                RowData.Value;

            return new StatModifier(
                RowData.StatType,
                RowData.OperationType,
                value);
        }

        public override object Clone()
        {
            return new StatBuff(this);
        }
    }
}
