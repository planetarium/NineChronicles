namespace Lib9c.Tests.Model.Item
{
    using System;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class ConsumableTest
    {
        private readonly ConsumableItemSheet.Row _consumableRow;

        public ConsumableTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _consumableRow = tableSheets.ConsumableItemSheet.First;
        }

        [Fact]
        public void Serialize()
        {
            Assert.NotNull(_consumableRow);

            var consumable = new Consumable(_consumableRow, Guid.NewGuid(), 0);
            var serialized = consumable.Serialize();
            var deserialized = new Consumable((Bencodex.Types.Dictionary)serialized);

            Assert.Equal(consumable, deserialized);
        }

        [Fact]
        public void Update()
        {
            var consumable = new Consumable(_consumableRow, Guid.NewGuid(), 0);
            consumable.Update(10);
            Assert.Equal(10, consumable.RequiredBlockIndex);
        }
    }
}
