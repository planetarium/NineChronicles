namespace Lib9c.Tests.TableData.Crystal
{
    using System.Linq;
    using Nekoyume.TableData.Crystal;
    using Xunit;

    public class CrystalRandomBuffSheetTest
    {
        [Fact]
        public void Constructor()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());

            Assert.NotNull(tableSheets.CrystalRandomBuffSheet);
        }

        [Fact]
        public void Set()
        {
            var sheet = new CrystalRandomBuffSheet();
            sheet.Set(@"buff_id,rank,ratio
1,SS,0.11");

            Assert.Single(sheet.Values);

            var row = sheet.Values.First();

            Assert.Equal(1, row.BuffId);
            Assert.Equal(CrystalRandomBuffSheet.Row.BuffRank.SS, row.Rank);
            Assert.Equal(0.11m, row.Ratio);
        }
    }
}
