namespace Lib9c.Tests.Model
{
    using System;
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

        public static Costume CreateFirstCostume(TableSheets tableSheets, Guid guid = default)
        {
            var row = tableSheets.CostumeItemSheet.First;
            Assert.NotNull(row);

            return new Costume(row, guid == default ? Guid.NewGuid() : guid);
        }

        [Fact]
        public void Serialize()
        {
            var guid = new Guid("F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4");
            var row = _tableSheets.CostumeItemSheet.Values.First();
            var costume = ItemFactory.CreateCostume(row, guid);
            Assert.Equal(guid, costume.ItemId);
            var serialized = costume.Serialize();
            var deserialized = new Costume((Dictionary)serialized);

            Assert.Equal(serialized, deserialized.Serialize());
        }
    }
}
