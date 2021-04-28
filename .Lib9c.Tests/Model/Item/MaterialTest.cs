namespace Lib9c.Tests.Model.Item
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class MaterialTest
    {
        private readonly MaterialItemSheet.Row _materialRow;

        public MaterialTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _materialRow = tableSheets.MaterialItemSheet.First;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Serialize(bool isTradable)
        {
            Assert.NotNull(_materialRow);

            var material = new Material(_materialRow, isTradable);
            var serialized = material.Serialize();
            var deserialized = new Material((Bencodex.Types.Dictionary)serialized);

            Assert.Equal(material, deserialized);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SerializeWithDotNetApi(bool isTradable)
        {
            Assert.NotNull(_materialRow);

            var material = new Material(_materialRow, isTradable);
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, material);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (Material)formatter.Deserialize(ms);

            Assert.Equal(material, deserialized);
        }

        [Fact]
        public void Equals_TradeId_When_ItemId_Is_Equals()
        {
            var material = new Material(_materialRow);
            var material2 = new Material(_materialRow, true);
            Assert.Equal(material.TradeId, material2.TradeId);
        }

        [Fact]
        public void Equals_With_Or_Without_IsTradable_When_IsTradable_False()
        {
            var material = new Material(_materialRow);
            var serialized = (Bencodex.Types.Dictionary)material.Serialize();
            var serializedWithoutIsTradable = serialized.ContainsKey("is_tradable")
                ? new Bencodex.Types.Dictionary(serialized.Remove((Bencodex.Types.Text)"is_tradable"))
                : serialized;
            Assert.Equal(serialized, serializedWithoutIsTradable);

            var deserialized = new Material(serialized);
            var deserializedWithoutIsTradable = new Material(serializedWithoutIsTradable);

            Assert.Equal(deserialized, deserializedWithoutIsTradable);
        }
    }
}
