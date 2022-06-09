namespace Lib9c.Tests.TableData.Crystal
{
    using System.Linq;
    using Nekoyume.TableData.Crystal;
    using Xunit;

    public class CrystalStageBuffGachaSheetTest
    {
        [Fact]
        public void Constructor()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());

            Assert.NotNull(tableSheets.CrystalStageBuffGachaSheet);
        }

        [Fact]
        public void Set()
        {
            var sheet = new CrystalStageBuffGachaSheet();
            sheet.Set(@"stage_id,max_star,normal_cost,advanced_cost
1,5,10,30");

            Assert.Single(sheet.Values);

            var row = sheet.Values.First();

            Assert.Equal(1, row.StageId);
            Assert.Equal(5, row.MaxStar);
            Assert.Equal(10, row.NormalCost);
            Assert.Equal(30, row.AdvancedCost);
        }
    }
}
