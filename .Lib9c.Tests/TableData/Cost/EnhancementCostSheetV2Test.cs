namespace Lib9c.Tests.TableData.Cost
{
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class EnhancementCostSheetV2Test
    {
        private const string _csv =
            @"id,item_sub_type,grade,level,cost,success_ratio,great_success_ratio,fail_ratio,success_required_block_index,great_success_required_block_index,fail_required_block_index
1,Weapon,1,1,0,0.75,0.25,0,300,700,20";

        [Fact]
        public void Set()
        {
            var sheet = new EnhancementCostSheetV2();
            sheet.Set(_csv);
            Assert.Single(sheet);
            Assert.NotNull(sheet.First);

            var first = sheet.First;
            Assert.Equal(first.Id, first.Key);
            Assert.Equal(1, first.Id);
            Assert.Equal(ItemSubType.Weapon, first.ItemSubType);
            Assert.Equal(1, first.Grade);
            Assert.Equal(1, first.Level);
            Assert.Equal(0, first.Cost);
            Assert.Equal(0.75m, first.SuccessRatio);
            Assert.Equal(0.25m, first.GreatSuccessRatio);
            Assert.Equal(0m, first.FailRatio);
            Assert.Equal(300, first.SuccessRequiredBlockIndex);
            Assert.Equal(700, first.GreatSuccessRequiredBlockIndex);
            Assert.Equal(20, first.FailRequiredBlockIndex);
        }
    }
}
