namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
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

        [Fact]
        public void Lock()
        {
            var costume = new Costume(_costumeRow, Guid.NewGuid());
            costume.Equip();
            costume.Lock(10);
            Assert.Equal(10, costume.RequiredBlockIndex);
            Assert.False(costume.equipped);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void LockThrowArgumentOutOfRangeException(long requiredBlockIndex)
        {
            var costume = new Costume(_costumeRow, Guid.NewGuid());
            Assert.True(requiredBlockIndex <= costume.RequiredBlockIndex);
            Assert.Throws<ArgumentOutOfRangeException>(() => costume.Lock(requiredBlockIndex));
        }

        [Fact]
        public void SerializeWithRequiredBlockIndex()
        {
            // Check RequiredBlockIndex 0 case;
            var costume = new Costume(_costumeRow, Guid.NewGuid());
            Dictionary serialized = (Dictionary)costume.Serialize();
            Assert.False(serialized.ContainsKey(Costume.RequiredBlockIndexKey));
            Assert.Equal(costume, new Costume(serialized));

            costume.Lock(1);
            serialized = (Dictionary)costume.Serialize();
            Assert.True(serialized.ContainsKey(Costume.RequiredBlockIndexKey));
            Assert.Equal(costume, new Costume(serialized));
        }

        [Fact]
        public void DeserializeThrowArgumentOurOfRangeException()
        {
            var costume = new Costume(_costumeRow, Guid.NewGuid());
            Assert.Equal(0, costume.RequiredBlockIndex);

            Dictionary serialized = (Dictionary)costume.Serialize();
            serialized = serialized.SetItem(Costume.RequiredBlockIndexKey, "-1");
            Assert.Throws<ArgumentOutOfRangeException>(() => new Costume(serialized));
        }
    }
}
