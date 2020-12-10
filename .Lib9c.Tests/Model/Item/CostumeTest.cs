namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class CostumeTest
    {
        private readonly CostumeItemSheet.Row _costumeRow;

        public CostumeTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _costumeRow = tableSheets.CostumeItemSheet.First;
        }

        [Fact]
        public void Serialize()
        {
            Assert.NotNull(_costumeRow);

            var costume = new Costume(_costumeRow, Guid.NewGuid());
            var serialized = costume.Serialize();
            var deserialized = new Costume((Bencodex.Types.Dictionary)serialized);

            Assert.Equal(costume, deserialized);
        }

        [Fact]
        public void SerializeWithDotNetAPI()
        {
            Assert.NotNull(_costumeRow);

            var costume = new Costume(_costumeRow, Guid.NewGuid());
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, costume);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (Costume)formatter.Deserialize(ms);

            Assert.Equal(costume, deserialized);
        }
    }
}
