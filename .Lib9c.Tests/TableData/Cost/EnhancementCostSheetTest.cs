namespace Lib9c.Tests.TableData.Cost
{
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class EnhancementCostSheetTest
    {
        private const string _csv = @"id,item_sub_type,grade,level,cost
1,Weapon,1,1,0";

        [Fact]
        public void Set()
        {
            var sheet = new EnhancementCostSheet();
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
        }
    }
}
