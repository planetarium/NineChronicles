namespace Lib9c.Tests.TableData.Crystal
{
    using System.Linq;
    using Nekoyume.TableData.Crystal;
    using Xunit;

    public class CrystalMonsterCollectionMultiplierSheetTest
    {
        private readonly CrystalMonsterCollectionMultiplierSheet _sheet;

        public CrystalMonsterCollectionMultiplierSheetTest()
        {
            _sheet = new TableSheets(TableSheetsImporter.ImportSheets())
                .CrystalMonsterCollectionMultiplierSheet;
        }

        [Fact]
        public void Set()
        {
            var row = _sheet.First().Value;

            Assert.Equal(0, row.Level);
            Assert.Equal(0, row.Multiplier);
        }
    }
}
