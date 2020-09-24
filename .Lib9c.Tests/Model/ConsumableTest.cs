namespace Lib9c.Tests.Model
{
    using System.Linq;
    using Bencodex.Types;
    using Nekoyume.Model.Item;
    using Xunit;

    public class ConsumableTest
    {
        private readonly TableSheets _tableSheets;

        public ConsumableTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Serialize()
        {
            var row = _tableSheets.ConsumableItemSheet.Values.First();
            var consumable = (Consumable)ItemFactory.CreateItemUsable(row, default, 0);
            var serialized = consumable.Serialize();
            var deserialized = new Consumable((Dictionary)serialized);

            Assert.Equal(serialized, deserialized.Serialize());
        }
    }
}
