namespace Lib9c.Tests.Model
{
    using System.Collections.Generic;
    using System.Linq;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Quest;
    using Xunit;

    public class QuestListTest
    {
        private readonly TableSheets _tableSheets;

        public QuestListTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void GetEnumerator()
        {
            var list = new QuestList(
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet
            ).ToList();

            for (var i = 0; i < list.Count() - 1; i++)
            {
                var quest = list[i];
                var next = list[i + 1];
                Assert.True(quest.Id < next.Id);
            }
        }

        [Fact]
        public void UpdateItemTypeCollectQuestDeterministic()
        {
            var expectedItemIds = new List<int>()
            {
                303000,
                303100,
                303200,
                306023,
                306040,
            };

            var itemIds = new List<int>()
            {
                400000,
            };
            itemIds.AddRange(expectedItemIds);

            var prevItems = itemIds
                .Select(id => ItemFactory.CreateMaterial(_tableSheets.MaterialItemSheet[id]))
                .Cast<ItemBase>()
                .OrderByDescending(i => i.Id)
                .ToList();

            Assert.Equal(6, prevItems.Count);

            var list = new QuestList(
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet
            );

            Assert.Empty(list.OfType<ItemTypeCollectQuest>().First().ItemIds);

            list.UpdateItemTypeCollectQuest(prevItems);

            Assert.Equal(expectedItemIds, list.OfType<ItemTypeCollectQuest>().First().ItemIds);
        }
    }
}
