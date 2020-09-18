namespace Lib9c.Tests.Model
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Quest;
    using Xunit;

    public class ItemTypeCollectQuestTest
    {
        private readonly TableSheets _tableSheets;

        public ItemTypeCollectQuestTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Serialize()
        {
            var quest = new ItemTypeCollectQuest(
                _tableSheets.ItemTypeCollectQuestSheet.Values.First(),
                new QuestReward(new Dictionary<int, int>())
            );

            var expected = new List<int>()
            {
                10110000,
                10111000,
            };

            foreach (var itemId in expected.OrderByDescending(i => i))
            {
                var item = ItemFactory.CreateItemUsable(_tableSheets.EquipmentItemSheet[itemId], default, 0);
                quest.Update(item);
            }

            Assert.Equal(expected, quest.ItemIds);

            var des = new ItemTypeCollectQuest((Dictionary)quest.Serialize());

            Assert.Equal(expected, des.ItemIds);

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, quest);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = (ItemTypeCollectQuest)formatter.Deserialize(ms);

            Assert.Equal(quest.ItemIds, deserialized.ItemIds);
        }
    }
}
