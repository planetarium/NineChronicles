namespace Lib9c.Tests.Model
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Quest;
    using Nekoyume.TableData;
    using Xunit;

    public class QuestListTest
    {
        private readonly TableSheets _tableSheets;

        public QuestListTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void SerializeExceptions() => ExceptionTest.AssertException(
            new UpdateListVersionException("test"),
            new UpdateListQuestsCountException("test"));

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

            for (var i = 0; i < list.Count - 1; i++)
            {
                var quest = list[i];
                var next = list[i + 1];
                Assert.True(quest.Id < next.Id);
            }
        }

        [Fact]
        public void UpdateItemTypeCollectQuestDeterministic()
        {
            var expectedItemIds = new List<int>
            {
                303000,
                303100,
                303200,
                306023,
                306040,
            };

            var itemIds = new List<int>
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

        [Theory]
        [InlineData(1)]
        [InlineData(99)]
        public void UpdateList(int questCountToAdd)
        {
            var questList = new QuestList(
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet
            );

            Assert.Equal(1, questList.ListVersion);
            Assert.Equal(_tableSheets.QuestSheet.Count, questList.Count());

            var questSheet = _tableSheets.QuestSheet;
            Assert.NotNull(questSheet.First);
            var patchedSheet = new WorldQuestSheet();
            var patchedSheetCsvSb = new StringBuilder().AppendLine("id,goal,quest_reward_id");
            for (var i = questCountToAdd; i > 0; i--)
            {
                patchedSheetCsvSb.AppendLine($"{990000 + i - 1},10,{questSheet.First.QuestRewardId}");
            }

            patchedSheet.Set(patchedSheetCsvSb.ToString());
            Assert.Equal(questCountToAdd, patchedSheet.Count);
            var previousQuestSheetCount = questSheet.Count;
            questSheet.Set(patchedSheet);
            Assert.Equal(previousQuestSheetCount + questCountToAdd, questSheet.Count);

            questList.UpdateList(
                questSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet);
            Assert.Equal(2, questList.ListVersion);
            Assert.Equal(questSheet.Count, questList.Count());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(99)]
        public void UpdateListV1(int questCountToAdd)
        {
            var questList = new QuestList(
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet
            );

            Assert.Equal(1, questList.ListVersion);
            Assert.Equal(_tableSheets.QuestSheet.Count, questList.Count());

            var questSheet = _tableSheets.QuestSheet;
            Assert.NotNull(questSheet.First);
            var patchedSheet = new WorldQuestSheet();
            var patchedSheetCsvSb = new StringBuilder().AppendLine("id,goal,quest_reward_id");
            for (var i = questCountToAdd; i > 0; i--)
            {
                patchedSheetCsvSb.AppendLine($"{990000 + i - 1},10,{questSheet.First.QuestRewardId}");
            }

            patchedSheet.Set(patchedSheetCsvSb.ToString());
            Assert.Equal(questCountToAdd, patchedSheet.Count);
            var previousQuestSheetCount = questSheet.Count;
            questSheet.Set(patchedSheet);
            Assert.Equal(previousQuestSheetCount + questCountToAdd, questSheet.Count);

            questList.UpdateListV1(
                2,
                questSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet);
            Assert.Equal(2, questList.ListVersion);
            Assert.Equal(questSheet.Count, questList.Count());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void UpdateList_Throw_UpdateListVersionException(int listVersion)
        {
            var questList = new QuestList(
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet
            );

            Assert.Equal(1, questList.ListVersion);
            Assert.Throws<UpdateListVersionException>(() =>
                questList.UpdateListV1(
                    listVersion,
                    _tableSheets.QuestSheet,
                    _tableSheets.QuestRewardSheet,
                    _tableSheets.QuestItemRewardSheet,
                    _tableSheets.EquipmentItemRecipeSheet));
        }

        [Fact]
        public void UpdateList_Throw_UpdateListQuestsCountException()
        {
            var questList = new QuestList(
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet
            );

            Assert.Equal(1, questList.ListVersion);
            Assert.Throws<UpdateListQuestsCountException>(() =>
                questList.UpdateListV1(
                    2,
                    _tableSheets.QuestSheet,
                    _tableSheets.QuestRewardSheet,
                    _tableSheets.QuestItemRewardSheet,
                    _tableSheets.EquipmentItemRecipeSheet));
        }
    }
}
