using System;
using System.Collections.Generic;
using Nekoyume.EnumType;

namespace Nekoyume.TableData
{
    [Serializable]
    public class MaterialItemSheet : Sheet<int, MaterialItemSheet.Row>
    {
        [Serializable]
        public class Row : ItemSheet.Row
        {
            public override ItemType ItemType => ItemType.Material;
            public string StatType { get; private set; }
            public int StatMin { get; private set; }
            public int StatMax { get; private set; }
            public int SkillId { get; private set; }
            public int SkillDamageMin { get; private set; }
            public int SkillDamageMax { get; private set; }
            public decimal SkillChanceMin { get; private set; }
            public decimal SkillChanceMax { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                StatType = fields[4];
                StatMin = string.IsNullOrEmpty(fields[5]) ? 0 : int.Parse(fields[5]);
                StatMax = string.IsNullOrEmpty(fields[6]) ? 0 : int.Parse(fields[6]);
                SkillId = string.IsNullOrEmpty(fields[7]) ? 0 : int.Parse(fields[7]);
                SkillDamageMin = string.IsNullOrEmpty(fields[8]) ? 0 : int.Parse(fields[8]);
                SkillDamageMax = string.IsNullOrEmpty(fields[9]) ? 0 : int.Parse(fields[9]);
                SkillChanceMin = string.IsNullOrEmpty(fields[10]) ? 0m : decimal.Parse(fields[10]);
                SkillChanceMax = string.IsNullOrEmpty(fields[11]) ? 0m : decimal.Parse(fields[11]);
            }
        }
    }
}
