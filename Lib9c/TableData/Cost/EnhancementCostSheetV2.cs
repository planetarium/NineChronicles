using System;
using System.Collections.Generic;
using System.Numerics;
using Nekoyume.Model.Item;

namespace Nekoyume.TableData
{
    using static TableExtensions;

    public class EnhancementCostSheetV2 : Sheet<int, EnhancementCostSheetV2.Row>
    {
        public class Row : EnhancementCostSheet.Row
        {
            public int SuccessRatio { get; private set; }
            public int GreatSuccessRatio { get; private set; }
            public int FailRatio { get; private set; }
            public int SuccessRequiredBlockIndex { get; private set; }
            public int GreatSuccessRequiredBlockIndex { get; private set; }
            public int FailRequiredBlockIndex { get; private set; }
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
                SuccessRatio = ParseInt(fields[5], 0);
                GreatSuccessRatio = ParseInt(fields[6], 0);
                FailRatio = ParseInt(fields[7], 0);
                SuccessRequiredBlockIndex = ParseInt(fields[8], 0);
                GreatSuccessRequiredBlockIndex = ParseInt(fields[9], 0);
                FailRequiredBlockIndex = ParseInt(fields[10], 0);
                BaseStatGrowthMin = ParseInt(fields[11], 0);
                BaseStatGrowthMax = ParseInt(fields[12], 0);
                ExtraStatGrowthMin = ParseInt(fields[13], 0);
                ExtraStatGrowthMax = ParseInt(fields[14], 0);
                ExtraSkillDamageGrowthMin = ParseInt(fields[15], 0);
                ExtraSkillDamageGrowthMax = ParseInt(fields[16], 0);
                ExtraSkillChanceGrowthMin = ParseInt(fields[17], 0);
                ExtraSkillChanceGrowthMax = ParseInt(fields[18], 0);
            }
        }

        public EnhancementCostSheetV2() : base(nameof(EnhancementCostSheetV2))
        {
        }
    }
}
