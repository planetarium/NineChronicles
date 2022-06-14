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
            sheet.Set(@"id,rank,skill_id,ratio
1,SS,400000,0.11");

            Assert.Single(sheet.Values);

            var row = sheet.Values.First();

            Assert.Equal(1, row.Id);
            Assert.Equal(CrystalRandomBuffSheet.Row.BuffRank.SS, row.Rank);
            Assert.Equal(400000, row.SkillId);
            Assert.Equal(0.11m, row.Ratio);
        }
    }
}
