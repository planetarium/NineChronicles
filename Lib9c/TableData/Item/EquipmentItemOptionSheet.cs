using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    public class EquipmentItemOptionSheet : Sheet<int, EquipmentItemOptionSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public StatType StatType { get; private set; }
            public int StatMin { get; private set; }
            public int StatMax { get; private set; }
            public int SkillId { get; private set; }
            public int SkillDamageMin { get; private set; }
            public int SkillDamageMax { get; private set; }
            public int SkillChanceMin { get; private set; }
            public int SkillChanceMax { get; private set; }
            public int StatDamageRatioMin { get; private set; }
            public int StatDamageRatioMax { get; private set; }
            public StatType ReferencedStatType { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                StatType = string.IsNullOrEmpty(fields[1])
                    ? StatType.NONE
                    : (StatType) Enum.Parse(typeof(StatType), fields[1]);
                StatMin = ParseInt(fields[2], 0);
                StatMax = ParseInt(fields[3], 0);
                SkillId = ParseInt(fields[4], 0);
                SkillDamageMin = ParseInt(fields[5], 0);
                SkillDamageMax = ParseInt(fields[6], 0);
                SkillChanceMin = ParseInt(fields[7], 0);
                SkillChanceMax = ParseInt(fields[8], 0);

                if (fields.Count > 9)
                {
                    StatDamageRatioMin = ParseInt(fields[9], 0);
                    StatDamageRatioMax = ParseInt(fields[10], 0);
                    ReferencedStatType = string.IsNullOrEmpty(fields[11])
                        ? StatType.NONE
                        : (StatType)Enum.Parse(typeof(StatType), fields[11]);
                }
            }
        }

        public EquipmentItemOptionSheet() : base(nameof(EquipmentItemOptionSheet))
        {
        }
    }
}
