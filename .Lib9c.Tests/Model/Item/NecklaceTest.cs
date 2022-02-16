namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class NecklaceTest
    {
        private readonly EquipmentItemSheet.Row _necklaceRow;

        public NecklaceTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _necklaceRow = tableSheets.EquipmentItemSheet.OrderedList.FirstOrDefault(row =>
                row.ItemSubType == ItemSubType.Necklace);
        }

        [Fact]
        public void Serialize()
        {
            Assert.NotNull(_necklaceRow);

            var costume = new Necklace(_necklaceRow, Guid.NewGuid(), 0);
            var serialized = costume.Serialize();
            var deserialized = new Necklace((Bencodex.Types.Dictionary)serialized);

            Assert.Equal(costume, deserialized);
        }

        [Fact]
        public void SerializeWithDotNetAPI()
        {
            Assert.NotNull(_necklaceRow);

            var costume = new Necklace(_necklaceRow, Guid.NewGuid(), 0);
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, costume);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (Necklace)formatter.Deserialize(ms);

            Assert.Equal(costume, deserialized);
        }
    }
}
