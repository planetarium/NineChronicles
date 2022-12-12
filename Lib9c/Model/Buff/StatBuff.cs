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

        protected StatBuff(StatBuffSheet.Row row) : base(
            new BuffInfo(row.Id, row.GroupId, row.Chance, row.Duration, row.TargetType))
        {
            RowData = row;
        }

        protected StatBuff(SkillCustomField customField, StatBuffSheet.Row row) : base(
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
            if (CustomField.HasValue)
            {
                var modifier = new StatModifier(
                    RowData.StatModifier.StatType,
                    RowData.StatModifier.Operation,
                    CustomField.Value.BuffValue);
                return modifier;
            }

            return RowData.StatModifier;
        }

        public override object Clone()
        {
            return new StatBuff(this);
        }
    }
}
