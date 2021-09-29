namespace Lib9c.Tests.TableData.Cost
{
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class EnhancementCostSheetTest
    {
        private const string _csv = @"id,item_sub_type,grade,level,cost
1,Weapon,1,1,0
2,Weapon,1,4,1000";

        [Fact]
        public void Set()
        {
            var sheet = new EnhancementCostSheet();
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

            row = sheet.Last;
            Assert.Equal(row.Id, row.Key);
            Assert.Equal(2, row.Id);
            Assert.Equal(ItemSubType.Weapon, row.ItemSubType);
            Assert.Equal(1, row.Grade);
            Assert.Equal(4, row.Level);
            Assert.Equal(1000, row.Cost);
        }
    }
}
