namespace Lib9c.Tests.TableData
{
    using System;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.TableData;
    using Xunit;

    public class RuneCostSheetTest
    {
        private readonly RuneCostSheet _runeCostSheetTest;

        public RuneCostSheetTest()
        {
            if (!TableSheetsImporter.TryGetCsv(nameof(RuneCostSheet), out var csv))
            {
                throw new Exception($"Not found sheet: {nameof(RuneCostSheet)}");
            }

            _runeCostSheetTest = new RuneCostSheet();
            _runeCostSheetTest.Set(csv);
        }

        [Fact]
        public void SetToSheet()
        {
            const string content =
                @"id,rune_level,rune_stone_id,rune_stone_quantity,crystal_quantity,ncg_quantity,level_up_success_rate
        250010001,1,1001,10,100,100,10000
        ";

            var sheet = new RuneCostSheet();
            sheet.Set(content);

            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
        }

        [Fact]
        public void GetRowTest()
        {
            var random = new TestRandom();
            var expectRow = _runeCostSheetTest.OrderedList[random.Next(0, _runeCostSheetTest.Count)];
            Assert.Equal(1, expectRow.Cost.First().Level);
            Assert.Equal(1001, expectRow.Cost.First().RuneStoneId);
            Assert.Equal(10, expectRow.Cost.First().RuneStoneQuantity);
            Assert.Equal(100, expectRow.Cost.First().CrystalQuantity);
            Assert.Equal(100, expectRow.Cost.First().NcgQuantity);
            Assert.Equal(10000, expectRow.Cost.First().LevelUpSuccessRate);
        }
    }
}
