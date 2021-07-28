namespace Lib9c.Tests.TableData.Cost
{
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class EnhancementCostSheetV2Test
    {
        private const string _csv =
            @"id,item_sub_type,grade,level,cost,success_ratio,great_success_ratio,fail_ratio,success_required_block_index,great_success_required_block_index,fail_required_block_index,base_stat_growth_min,base_stat_growth_max,extra_stat_growth_min,extra_stat_growth_max,extra_skill_damage_growth_min,extra_skill_damage_growth_max,extra_skill_proc_growth_min,extra_skill_proc_growth_max
1,Weapon,1,1,0,0.75,0.25,0,300,700,50,0.08,0.12,0.24,0.36,0.24,0.36,0.12,0.18
2,Weapon,1,2,0,0.75,0.25,0,300,700,50,0.08,0.12,0.08,0.12,0.08,0.12,0.04,0.06";

        [Fact]
        public void Set()
        {
            var sheet = new EnhancementCostSheetV2();
            sheet.Set(_csv);
            Assert.Equal(2, sheet.Count);
            Assert.NotNull(sheet.First);
            Assert.NotNull(sheet.Last);

            var row = sheet.First;
            Assert.Equal(row.Id, row.Key);
            Assert.Equal(1, row.Id);
            Assert.Equal(ItemSubType.Weapon, row.ItemSubType);
            Assert.Equal(1, row.Grade);
            Assert.Equal(1, row.Level);
            Assert.Equal(0, row.Cost);
            Assert.Equal(0.75m, row.SuccessRatio);
            Assert.Equal(0.25m, row.GreatSuccessRatio);
            Assert.Equal(0m, row.FailRatio);
            Assert.Equal(300, row.SuccessRequiredBlockIndex);
            Assert.Equal(700, row.GreatSuccessRequiredBlockIndex);
            Assert.Equal(50, row.FailRequiredBlockIndex);
            Assert.Equal(0.08m, row.BaseStatGrowthMin);
            Assert.Equal(0.12m, row.BaseStatGrowthMax);
            Assert.Equal(0.24m, row.ExtraStatGrowthMin);
            Assert.Equal(0.36m, row.ExtraStatGrowthMax);
            Assert.Equal(0.24m, row.ExtraSkillDamageGrowthMin);
            Assert.Equal(0.36m, row.ExtraSkillDamageGrowthMax);
            Assert.Equal(0.12m, row.ExtraSkillProcGrowthMin);
            Assert.Equal(0.18m, row.ExtraSkillProcGrowthMax);

            row = sheet.Last;
            Assert.Equal(row.Id, row.Key);
            Assert.Equal(2, row.Id);
            Assert.Equal(ItemSubType.Weapon, row.ItemSubType);
            Assert.Equal(1, row.Grade);
            Assert.Equal(2, row.Level);
            Assert.Equal(0, row.Cost);
            Assert.Equal(0.75m, row.SuccessRatio);
            Assert.Equal(0.25m, row.GreatSuccessRatio);
            Assert.Equal(0m, row.FailRatio);
            Assert.Equal(300, row.SuccessRequiredBlockIndex);
            Assert.Equal(700, row.GreatSuccessRequiredBlockIndex);
            Assert.Equal(50, row.FailRequiredBlockIndex);
            Assert.Equal(0.08m, row.BaseStatGrowthMin);
            Assert.Equal(0.12m, row.BaseStatGrowthMax);
            Assert.Equal(0.08m, row.ExtraStatGrowthMin);
            Assert.Equal(0.12m, row.ExtraStatGrowthMax);
            Assert.Equal(0.08m, row.ExtraSkillDamageGrowthMin);
            Assert.Equal(0.12m, row.ExtraSkillDamageGrowthMax);
            Assert.Equal(0.04m, row.ExtraSkillProcGrowthMin);
            Assert.Equal(0.06m, row.ExtraSkillProcGrowthMax);
        }
    }
}
