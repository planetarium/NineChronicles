namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class CostumeTest
    {
        private readonly CostumeItemSheet.Row _costumeRow;

        public CostumeTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _costumeRow = tableSheets.CostumeItemSheet.First;
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
        public void Update()
        {
            var costume = new Costume(_costumeRow, Guid.NewGuid());
            costume.Equip();
            costume.Update(10);
            Assert.Equal(10, costume.RequiredBlockIndex);
            Assert.False(costume.equipped);
        }

        [Fact]
        public void LockThrowArgumentOutOfRangeException()
        {
            var costume = new Costume(_costumeRow, Guid.NewGuid());
            Assert.True(costume.RequiredBlockIndex >= -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => costume.Update(-1));
        }

        [Fact]
        public void SerializeWithRequiredBlockIndex()
        {
            // Check RequiredBlockIndex 0 case;
            var costume = new Costume(_costumeRow, Guid.NewGuid());
            Dictionary serialized = (Dictionary)costume.Serialize();
            Assert.False(serialized.ContainsKey(RequiredBlockIndexKey));
            Assert.Equal(costume, new Costume(serialized));

            costume.Update(1);
            serialized = (Dictionary)costume.Serialize();
            Assert.True(serialized.ContainsKey(RequiredBlockIndexKey));
            Assert.Equal(costume, new Costume(serialized));
        }

        [Fact]
        public void DeserializeThrowArgumentOurOfRangeException()
        {
            var costume = new Costume(_costumeRow, Guid.NewGuid());
            Assert.Equal(0, costume.RequiredBlockIndex);

            Dictionary serialized = (Dictionary)costume.Serialize();
            serialized = serialized.SetItem(RequiredBlockIndexKey, "-1");
            Assert.Throws<ArgumentOutOfRangeException>(() => new Costume(serialized));
        }
    }
}
