namespace Lib9c.Tests.Model.Item
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class ChestTest
    {
        private readonly MaterialItemSheet.Row _chestRow;

        public ChestTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _chestRow = tableSheets.MaterialItemSheet.OrderedList.FirstOrDefault(row =>
                row.ItemSubType == ItemSubType.Chest);
        }

        [Fact]
        public void Serialize()
        {
            Assert.NotNull(_chestRow);

            var chest = new Chest(_chestRow, new List<RedeemRewardSheet.RewardInfo>());
            var serialized = chest.Serialize();
            var deserialized = new Chest((Bencodex.Types.Dictionary)serialized);

            Assert.Equal(chest, deserialized);
        }

        [Fact]
        public void SerializeWithDotNetAPI()
        {
            Assert.NotNull(_chestRow);

            var chest = new Chest(_chestRow, new List<RedeemRewardSheet.RewardInfo>());
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, chest);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (Chest)formatter.Deserialize(ms);

            Assert.Equal(chest, deserialized);
        }
    }
}
