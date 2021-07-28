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
            public decimal SuccessRatio { get; private set; }
            public decimal GreatSuccessRatio { get; private set; }
            public decimal FailRatio { get; private set; }
            public int SuccessRequiredBlockIndex { get; private set; }
            public int GreatSuccessRequiredBlockIndex { get; private set; }
            public int FailRequiredBlockIndex { get; private set; }
            public decimal BaseStatGrowthMin { get; private set; }
            public decimal BaseStatGrowthMax { get; private set; }
            public decimal ExtraStatGrowthMin { get; private set; }
            public decimal ExtraStatGrowthMax { get; private set; }
            public decimal ExtraSkillDamageGrowthMin { get; private set; }
            public decimal ExtraSkillDamageGrowthMax { get; private set; }
            public decimal ExtraSkillProcGrowthMin { get; private set; }
            public decimal ExtraSkillProcGrowthMax { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                SuccessRatio = ParseDecimal(fields[5], 0m);
                GreatSuccessRatio = ParseDecimal(fields[6], 0m);
                FailRatio = ParseDecimal(fields[7], 0m);
                SuccessRequiredBlockIndex = ParseInt(fields[8], 0);
                GreatSuccessRequiredBlockIndex = ParseInt(fields[9], 0);
                FailRequiredBlockIndex = ParseInt(fields[10], 0);
                BaseStatGrowthMin = ParseDecimal(fields[11], 0m);
                BaseStatGrowthMax = ParseDecimal(fields[12], 0m);
                ExtraStatGrowthMin = ParseDecimal(fields[13], 0m);
                ExtraStatGrowthMax = ParseDecimal(fields[14], 0m);
                ExtraSkillDamageGrowthMin = ParseDecimal(fields[15], 0m);
                ExtraSkillDamageGrowthMax = ParseDecimal(fields[16], 0m);
                ExtraSkillProcGrowthMin = ParseDecimal(fields[17], 0m);
                ExtraSkillProcGrowthMax = ParseDecimal(fields[18], 0m);
            }
        }

        public EnhancementCostSheetV2() : base(nameof(EnhancementCostSheetV2))
        {
        }
    }
}
