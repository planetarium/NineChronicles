namespace Lib9c.Tests.TableData.Crystal
{
    using System.Linq;
    using Nekoyume.TableData.Crystal;
    using Xunit;

    public class CrystalEquipmentGrindingSheetTest
    {
        private readonly CrystalEquipmentGrindingSheet _sheet;

        public CrystalEquipmentGrindingSheetTest()
        {
            _sheet = new TableSheets(TableSheetsImporter.ImportSheets())
                .CrystalEquipmentGrindingSheet;
        }

        [Fact]
        public void Set()
        {
            var row = _sheet.First().Value;

            Assert.Equal(10100000, row.ItemId);
            Assert.Equal(100, row.CRYSTAL);
        }
    }
}
