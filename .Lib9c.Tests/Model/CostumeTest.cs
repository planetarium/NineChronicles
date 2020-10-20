namespace Lib9c.Tests.Model
{
    using System.Linq;
    using Bencodex.Types;
    using Nekoyume.Model.Item;
    using Xunit;

    public class CostumeTest
    {
        private readonly TableSheets _tableSheets;

        public CostumeTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Serialize()
        {
            var row = _tableSheets.CostumeItemSheet.Values.First();
            var statRow = _tableSheets.CostumeStatSheet.Values.First();
            var costume = ItemFactory.CreateCostume(row);
            var serialized = costume.Serialize();
            var deserialized = new Costume((Dictionary)serialized);

            Assert.Equal(serialized, deserialized.Serialize());
        }
    }
}
