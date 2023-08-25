using System;
using System.Collections.Generic;
using System.Numerics;
using Nekoyume.Model.Item;

namespace Nekoyume.TableData
{
    using static TableExtensions;

    public class EnhancementCostSheetV3 : Sheet<int, EnhancementCostSheetV3.Row>
    {
        public class Row : EnhancementCostSheet.Row
        {
            public long Exp { get; private set; }
            public int RequiredBlockIndex { get; private set; }
            public int BaseStatGrowthMin { get; private set; }
            public int BaseStatGrowthMax { get; private set; }
            public int ExtraStatGrowthMin { get; private set; }
            public int ExtraStatGrowthMax { get; private set; }
            public int ExtraSkillDamageGrowthMin { get; private set; }
            public int ExtraSkillDamageGrowthMax { get; private set; }
            public int ExtraSkillChanceGrowthMin { get; private set; }
            public int ExtraSkillChanceGrowthMax { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                Exp = ParseLong(fields[5], 0);
                RequiredBlockIndex = ParseInt(fields[6], 0);
                BaseStatGrowthMin = ParseInt(fields[7], 0);
                BaseStatGrowthMax = ParseInt(fields[8], 0);
                ExtraStatGrowthMin = ParseInt(fields[9], 0);
                ExtraStatGrowthMax = ParseInt(fields[10], 0);
                ExtraSkillDamageGrowthMin = ParseInt(fields[11], 0);
                ExtraSkillDamageGrowthMax = ParseInt(fields[12], 0);
                ExtraSkillChanceGrowthMin = ParseInt(fields[13], 0);
                ExtraSkillChanceGrowthMax = ParseInt(fields[14], 0);
            }
        }

        public EnhancementCostSheetV3() : base(nameof(EnhancementCostSheetV2))
        {
        }
    }
}
