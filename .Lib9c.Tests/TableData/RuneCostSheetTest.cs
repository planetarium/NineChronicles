namespace Lib9c.Tests.TableData
{
    using System;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.TableData;
    using Xunit;

    public class RuneCostSheetTest
    {
        [Fact]
        public void SetToSheet()
        {
            const string content =
                @"id,rune_level,rune_stone_quantity,crystal_quantity,ncg_quantity,level_up_success_rate
        1001,1,10,100,100,10000
        ";

            var sheet = new RuneCostSheet();
            sheet.Set(content);

            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
        }

        [Fact]
        public void GetRowTest()
        {
            const string content =
                @"id,rune_level,rune_stone_quantity,crystal_quantity,ncg_quantity,level_up_success_rate
        1001,1,10,100,100,10000
        ";

            var sheet = new RuneCostSheet();
            sheet.Set(content);

            var random = new TestRandom();
            var expectRow = sheet.OrderedList[random.Next(0, sheet.Count)];
            Assert.Equal(1, expectRow.Cost.First().Level);
            Assert.Equal(10, expectRow.Cost.First().RuneStoneQuantity);
            Assert.Equal(100, expectRow.Cost.First().CrystalQuantity);
            Assert.Equal(100, expectRow.Cost.First().NcgQuantity);
            Assert.Equal(10000, expectRow.Cost.First().LevelUpSuccessRate);
        }
    }
}
