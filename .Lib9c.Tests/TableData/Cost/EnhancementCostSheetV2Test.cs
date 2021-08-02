namespace Lib9c.Tests.TableData.Cost
{
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class EnhancementCostSheetV2Test
    {
        private const string _csv =
            @"id,item_sub_type,grade,level,cost,success_ratio,great_success_ratio,fail_ratio,success_required_block_index,great_success_required_block_index,fail_required_block_index,base_stat_growth_min,base_stat_growth_max,extra_stat_growth_min,extra_stat_growth_max,extra_skill_damage_growth_min,extra_skill_damage_growth_max,extra_skill_chance_growth_min,extra_skill_chance_growth_max
1,Weapon,1,1,0,7500,2500,0,300,700,50,800,1200,2400,3600,2400,3600,1200,1800
2,Weapon,1,2,0,7500,2500,0,300,700,50,800,1200,800,1200,800,1200,400,600";

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
            Assert.Equal(7500, row.SuccessRatio);
            Assert.Equal(2500, row.GreatSuccessRatio);
            Assert.Equal(0, row.FailRatio);
            Assert.Equal(300, row.SuccessRequiredBlockIndex);
            Assert.Equal(700, row.GreatSuccessRequiredBlockIndex);
            Assert.Equal(50, row.FailRequiredBlockIndex);
            Assert.Equal(800, row.BaseStatGrowthMin);
            Assert.Equal(01200, row.BaseStatGrowthMax);
            Assert.Equal(02400, row.ExtraStatGrowthMin);
            Assert.Equal(03600, row.ExtraStatGrowthMax);
            Assert.Equal(02400, row.ExtraSkillDamageGrowthMin);
            Assert.Equal(03600, row.ExtraSkillDamageGrowthMax);
            Assert.Equal(01200, row.ExtraSkillChanceGrowthMin);
            Assert.Equal(01800, row.ExtraSkillChanceGrowthMax);

            row = sheet.Last;
            Assert.Equal(row.Id, row.Key);
            Assert.Equal(2, row.Id);
            Assert.Equal(ItemSubType.Weapon, row.ItemSubType);
            Assert.Equal(1, row.Grade);
            Assert.Equal(2, row.Level);
            Assert.Equal(0, row.Cost);
            Assert.Equal(7500, row.SuccessRatio);
            Assert.Equal(2500, row.GreatSuccessRatio);
            Assert.Equal(0, row.FailRatio);
            Assert.Equal(300, row.SuccessRequiredBlockIndex);
            Assert.Equal(700, row.GreatSuccessRequiredBlockIndex);
            Assert.Equal(50, row.FailRequiredBlockIndex);
            Assert.Equal(800, row.BaseStatGrowthMin);
            Assert.Equal(1200, row.BaseStatGrowthMax);
            Assert.Equal(0800, row.ExtraStatGrowthMin);
            Assert.Equal(1200, row.ExtraStatGrowthMax);
            Assert.Equal(0800, row.ExtraSkillDamageGrowthMin);
            Assert.Equal(1200, row.ExtraSkillDamageGrowthMax);
            Assert.Equal(0400, row.ExtraSkillChanceGrowthMin);
            Assert.Equal(0600, row.ExtraSkillChanceGrowthMax);
        }
    }
}
