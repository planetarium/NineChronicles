namespace Lib9c.Tests.Model.Item
{
    using System.Collections.Generic;
    using System.Linq;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class ChestTest
    {
        private readonly MaterialItemSheet.Row _chestRow;

        public ChestTest()
        {
            var dict = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(dict);
            _chestRow = tableSheets.MaterialItemSheet.OrderedList.FirstOrDefault(row =>
                row.ItemSubType == ItemSubType.Chest);
        }

        [Fact]
        public void Serialization()
        {
            Assert.NotNull(_chestRow);

            var chest = new Chest(_chestRow, new List<RedeemRewardSheet.RewardInfo>());
            var serialized = chest.Serialize();
            var deserialized = new Chest((Bencodex.Types.Dictionary)serialized);

            Assert.Equal(chest, deserialized);
        }
    }
}
