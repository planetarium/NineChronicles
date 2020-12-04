namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
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

            var costume = new Consumable(_consumableRow, Guid.NewGuid(), 0);
            var serialized = costume.Serialize();
            var deserialized = new Consumable((Bencodex.Types.Dictionary)serialized);

            Assert.Equal(costume, deserialized);
        }

        [Fact]
        public void SerializeWithDotNetAPI()
        {
            Assert.NotNull(_consumableRow);

            var costume = new Consumable(_consumableRow, Guid.NewGuid(), 0);
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, costume);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (Consumable)formatter.Deserialize(ms);

            Assert.Equal(costume, deserialized);
        }
    }
}
