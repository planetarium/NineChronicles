namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class BeltTest
    {
        private readonly EquipmentItemSheet.Row _beltRow;

        public BeltTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _beltRow = tableSheets.EquipmentItemSheet.OrderedList.FirstOrDefault(row =>
                row.ItemSubType == ItemSubType.Belt);
        }

        [Fact]
        public void Serialize()
        {
            Assert.NotNull(_beltRow);

            var costume = new Belt(_beltRow, Guid.NewGuid(), 0);
            var serialized = costume.Serialize();
            var deserialized = new Belt((Bencodex.Types.Dictionary)serialized);

            Assert.Equal(costume, deserialized);
        }

        [Fact]
        public void SerializeWithDotNetAPI()
        {
            Assert.NotNull(_beltRow);

            var costume = new Belt(_beltRow, Guid.NewGuid(), 0);
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, costume);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (Belt)formatter.Deserialize(ms);

            Assert.Equal(costume, deserialized);
        }
    }
}
