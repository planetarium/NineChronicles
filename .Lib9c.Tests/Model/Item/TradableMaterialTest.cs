namespace Lib9c.Tests.Model.Item
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class TradableMaterialTest
    {
        private readonly MaterialItemSheet.Row _materialRow;

        public TradableMaterialTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _materialRow = tableSheets.MaterialItemSheet.First;
        }

        [Fact]
        public void Serialize()
        {
            Assert.NotNull(_materialRow);

            var material = new TradableMaterial(_materialRow);
            var serialized = material.Serialize();
            var deserialized = new TradableMaterial((Bencodex.Types.Dictionary)serialized);

            Assert.Equal(material, deserialized);
        }

        [Fact]
        public void SerializeWithDotNetApi()
        {
            Assert.NotNull(_materialRow);

            var material = new TradableMaterial(_materialRow);
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, material);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (TradableMaterial)formatter.Deserialize(ms);

            Assert.Equal(material, deserialized);
        }
    }
}
