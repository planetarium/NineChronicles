namespace Lib9c.Tests.TableData.Cost
{
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class EnhancementCostSheetV2Test
    {
        private const string _csv =
            @"id,item_sub_type,grade,level,cost,success_ratio,great_success_ratio,fail_ratio,success_required_block_index,great_success_required_block_index,fail_required_block_index
1,Weapon,1,1,0,0.75,0.25,0,300,700,20
2,Weapon,1,7,500,0.67,0.23,0.1,300,700,20";

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
            Assert.Equal(20, row.FailRequiredBlockIndex);

            row = sheet.Last;
            Assert.Equal(row.Id, row.Key);
            Assert.Equal(2, row.Id);
            Assert.Equal(ItemSubType.Weapon, row.ItemSubType);
            Assert.Equal(1, row.Grade);
            Assert.Equal(7, row.Level);
            Assert.Equal(500, row.Cost);
            Assert.Equal(0.67m, row.SuccessRatio);
            Assert.Equal(0.23m, row.GreatSuccessRatio);
            Assert.Equal(0.1m, row.FailRatio);
            Assert.Equal(300, row.SuccessRequiredBlockIndex);
            Assert.Equal(700, row.GreatSuccessRequiredBlockIndex);
            Assert.Equal(20, row.FailRequiredBlockIndex);
        }
    }
}
