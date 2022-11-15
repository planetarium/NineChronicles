namespace Lib9c.Tests.TableData.GrandFinale
{
    using System;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.TableData.GrandFinale;
    using Xunit;

    public class GrandFinaleScheduleSheetTest
    {
        private readonly GrandFinaleScheduleSheet _sheet;

        public GrandFinaleScheduleSheetTest()
        {
            if (!TableSheetsImporter.TryGetCsv(nameof(GrandFinaleScheduleSheet), out var csv))
            {
                throw new Exception($"Not found sheet: {nameof(GrandFinaleScheduleSheet)}");
            }

            _sheet = new GrandFinaleScheduleSheet();
            _sheet.Set(csv);
        }

        [Fact]
        public void SetToSheet()
        {
            const string tableContent = @"id,start_block_index,end_block_index
1,1,10";
            var sheet = new GrandFinaleScheduleSheet();
            sheet.Set(tableContent);
            Assert.NotNull(sheet.First);
            Assert.Equal(1, sheet.First.Id);
            Assert.Equal(1, sheet.First.StartBlockIndex);
            Assert.Equal(10, sheet.First.EndBlockIndex);
        }

        [Fact]
        public void GetRowByBlockIndex()
        {
            var random = new TestRandom();
            var expectRow = _sheet.OrderedList[random.Next(0, _sheet.Count)];
            var blockIndex = expectRow.StartBlockIndex;
            var row = _sheet.GetRowByBlockIndex(blockIndex);
            Assert.NotNull(row);
            Assert.Equal(expectRow.Id, row.Id);
            Assert.Equal(expectRow.StartBlockIndex, row.StartBlockIndex);
            Assert.Equal(expectRow.EndBlockIndex, row.EndBlockIndex);

            blockIndex = _sheet.OrderedList.OrderBy(r => r.EndBlockIndex).Last().EndBlockIndex + 1;
            Assert.Null(_sheet.GetRowByBlockIndex(blockIndex));
        }
    }
}
