using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex.Types;
using Libplanet.Assets;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Quest
{
    [Serializable]
    public class UpdateListVersionException : ArgumentOutOfRangeException
    {
        public UpdateListVersionException()
        {
        }

        public UpdateListVersionException(string s) : base(s)
        {
        }

        public UpdateListVersionException(int expected, int actual)
            : base($"{nameof(expected)}: {expected}, {nameof(actual)}: {actual}")
        {
        }

        protected UpdateListVersionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class UpdateListQuestsCountException : ArgumentException
    {
        public UpdateListQuestsCountException()
        {
        }

        public UpdateListQuestsCountException(string s) : base(s)
        {
        }

        public UpdateListQuestsCountException(int expected, int actual)
            : base($"{nameof(expected)}: greater than {expected}, {nameof(actual)}: {actual}")
        {
        }

        protected UpdateListQuestsCountException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class QuestList : IEnumerable<Quest>, IState
    {
        private const string _questsKeyDeprecated = "quests";
        private const string _questsKey = "q";
        private readonly List<Quest> _quests;

        private const string _listVersionKey = "lv";
        private int _listVersion = 1;
        public int ListVersion => _listVersion;

        private const string _completedQuestIdsKeyDeprecated = "completedQuestIds";
        private const string _completedQuestIdsKey = "cqi";
        public List<int> completedQuestIds = new List<int>();

        public QuestList(QuestSheet questSheet,
            QuestRewardSheet questRewardSheet,
            QuestItemRewardSheet questItemRewardSheet,
            EquipmentItemRecipeSheet equipmentItemRecipeSheet,
            EquipmentItemSubRecipeSheet equipmentItemSubRecipeSheet
        )
        {
            _quests = new List<Quest>();
            foreach (var questData in questSheet.OrderedList)
            {
                Quest quest;
                QuestReward reward = GetQuestReward(
                    questData.QuestRewardId,
                    questRewardSheet,
                    questItemRewardSheet
                );
                switch (questData)
                {
                    case CollectQuestSheet.Row row:
                        quest = new CollectQuest(row, reward);
                        _quests.Add(quest);
                        break;
                    case CombinationQuestSheet.Row row1:
                        quest = new CombinationQuest(row1, reward);
                        _quests.Add(quest);
                        break;
                    case GeneralQuestSheet.Row row2:
                        quest = new GeneralQuest(row2, reward);
                        _quests.Add(quest);
                        break;
                    case ItemEnhancementQuestSheet.Row row3:
                        quest = new ItemEnhancementQuest(row3, reward);
                        _quests.Add(quest);
                        break;
                    case ItemGradeQuestSheet.Row row4:
                        quest = new ItemGradeQuest(row4, reward);
                        _quests.Add(quest);
                        break;
                    case MonsterQuestSheet.Row row5:
                        quest = new MonsterQuest(row5, reward);
                        _quests.Add(quest);
                        break;
                    case TradeQuestSheet.Row row6:
                        quest = new TradeQuest(row6, reward);
                        _quests.Add(quest);
                        break;
                    case WorldQuestSheet.Row row7:
                        quest = new WorldQuest(row7, reward);
                        _quests.Add(quest);
                        break;
                    case ItemTypeCollectQuestSheet.Row row8:
                        quest = new ItemTypeCollectQuest(row8, reward);
                        _quests.Add(quest);
                        break;
                    case GoldQuestSheet.Row row9:
                        quest = new GoldQuest(row9, reward);
                        _quests.Add(quest);
                        break;
                    case CombinationEquipmentQuestSheet.Row row10:
                        int stageId;
                        var recipeRow = equipmentItemRecipeSheet.Values
                            .FirstOrDefault(r => r.Id == row10.RecipeId);
                        if (recipeRow is null)
                        {
                            throw new ArgumentException($"Invalid Recipe Id : {row10.RecipeId}");
                        }

                        stageId = recipeRow.UnlockStage;
                        quest = new CombinationEquipmentQuest(row10, reward, stageId);
                        _quests.Add(quest);
                        break;
                }
            }
        }


        public QuestList(Dictionary serialized)
        {
            _listVersion = serialized.TryGetValue((Text) _listVersionKey, out var listVersion)
                ? listVersion.ToInteger()
                : 1;

            switch (_listVersion)
            {
                case 1:
                {
                    _quests = serialized.TryGetValue((Text) _questsKeyDeprecated, out var questsValue)
                        ? questsValue.ToList(Quest.Deserialize)
                        : new List<Quest>();

                    completedQuestIds = serialized.TryGetValue((Text) _completedQuestIdsKeyDeprecated, out var idsValue)
                        ? idsValue.ToList(StateExtensions.ToInteger)
                        : new List<int>();
                    break;
                }
                case 2:
                {
                    _quests = serialized.TryGetValue((Text) _questsKey, out var q)
                        ? q.ToList(Quest.Deserialize)
                        : new List<Quest>();

                    completedQuestIds = serialized.TryGetValue((Text) _completedQuestIdsKey, out var cqi)
                        ? cqi.ToList(StateExtensions.ToInteger)
                        : new List<int>();
                    break;
                }
            }
        }

        public void UpdateList(
            int listVersion,
            QuestSheet questSheet,
            QuestRewardSheet questRewardSheet,
            QuestItemRewardSheet questItemRewardSheet,
            EquipmentItemRecipeSheet equipmentItemRecipeSheet)
        {
            if (listVersion != _listVersion + 1)
            {
                throw new UpdateListVersionException(_listVersion + 1, listVersion);
            }

            if (questSheet.Count <= _quests.Count)
            {
                throw new UpdateListQuestsCountException(_quests.Count, questSheet.Count);
            }

            _listVersion = listVersion;

            for (var i = questSheet.OrderedList.Count; i > 0; i--)
            {
                var questRow = questSheet.OrderedList[i - 1];
                var quest = _quests.FirstOrDefault(e => e.Id == questRow.Id);
                if (!(quest is null))
                {
                    continue;
                }

                var reward = GetQuestReward(
                    questRow.QuestRewardId,
                    questRewardSheet,
                    questItemRewardSheet);

                switch (questRow)
                {
                    case CollectQuestSheet.Row row:
                        quest = new CollectQuest(row, reward);
                        break;
                    case CombinationQuestSheet.Row row1:
                        quest = new CombinationQuest(row1, reward);
                        break;
                    case GeneralQuestSheet.Row row2:
                        quest = new GeneralQuest(row2, reward);
                        break;
                    case ItemEnhancementQuestSheet.Row row3:
                        quest = new ItemEnhancementQuest(row3, reward);
                        break;
                    case ItemGradeQuestSheet.Row row4:
                        quest = new ItemGradeQuest(row4, reward);
                        break;
                    case MonsterQuestSheet.Row row5:
                        quest = new MonsterQuest(row5, reward);
                        break;
                    case TradeQuestSheet.Row row6:
                        quest = new TradeQuest(row6, reward);
                        break;
                    case WorldQuestSheet.Row row7:
                        quest = new WorldQuest(row7, reward);
                        break;
                    case ItemTypeCollectQuestSheet.Row row8:
                        quest = new ItemTypeCollectQuest(row8, reward);
                        break;
                    case GoldQuestSheet.Row row9:
                        quest = new GoldQuest(row9, reward);
                        break;
                    case CombinationEquipmentQuestSheet.Row row10:
                        int stageId;
                        var recipeRow = equipmentItemRecipeSheet.Values
                            .FirstOrDefault(r => r.Id == row10.RecipeId);
                        if (recipeRow is null)
                        {
                            throw new ArgumentException($"Invalid Recipe Id : {row10.RecipeId}");
                        }

                        stageId = recipeRow.UnlockStage;
                        quest = new CombinationEquipmentQuest(row10, reward, stageId);
                        break;
                    default:
                        continue;
                }

                _quests.Add(quest);
            }
        }

        public IEnumerator<Quest> GetEnumerator()
        {
            return _quests.OrderBy(q => q.Id).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void UpdateCombinationQuest(ItemUsable itemUsable)
        {
            var targets = _quests
                .OfType<CombinationQuest>()
                .Where(i => i.ItemType == itemUsable.ItemType &&
                            i.ItemSubType == itemUsable.ItemSubType &&
                            !i.Complete);
            foreach (var target in targets)
            {
                target.Update(new List<ItemBase> {itemUsable});
            }
        }

        public void UpdateTradeQuest(TradeType type, FungibleAssetValue price)
        {
            var tradeQuests = _quests
                .OfType<TradeQuest>()
                .Where(i => i.Type == type && !i.Complete);
            foreach (var tradeQuest in tradeQuests)
            {
                tradeQuest.Check();
            }

            var goldQuests = _quests
                .OfType<GoldQuest>()
                .Where(i => i.Type == type && !i.Complete);
            foreach (var goldQuest in goldQuests)
            {
                goldQuest.Update(price);
            }
        }

        public void UpdateStageQuest(CollectionMap stageMap)
        {
            var stageQuests = _quests.OfType<WorldQuest>();
            foreach (var quest in stageQuests)
            {
                quest.Update(stageMap);
            }
        }

        public void UpdateMonsterQuest(CollectionMap monsterMap)
        {
            var monsterQuests = _quests.OfType<MonsterQuest>();
            foreach (var quest in monsterQuests)
            {
                quest.Update(monsterMap);
            }
        }

        public void UpdateCollectQuest(CollectionMap itemMap)
        {
            var collectQuests = _quests.OfType<CollectQuest>();
            foreach (var quest in collectQuests)
            {
                quest.Update(itemMap);
            }
        }

        public void UpdateItemEnhancementQuest(Equipment equipment)
        {
            var targets = _quests
                .OfType<ItemEnhancementQuest>()
                .Where(i => !i.Complete && i.Grade == equipment.Grade);
            foreach (var target in targets)
            {
                target.Update(equipment);
            }
        }

        public CollectionMap UpdateGeneralQuest(IEnumerable<QuestEventType> types,
            CollectionMap eventMap)
        {
            foreach (var type in types)
            {
                var targets = _quests
                    .OfType<GeneralQuest>()
                    .Where(i => i.Event == type && !i.Complete);
                foreach (var target in targets)
                {
                    target.Update(eventMap);
                }
            }

            return eventMap;
        }

        public void UpdateItemGradeQuest(ItemUsable itemUsable)
        {
            var targets = _quests
                .OfType<ItemGradeQuest>()
                .Where(i => i.Grade == itemUsable.Grade && !i.Complete);
            foreach (var target in targets)
            {
                target.Update(itemUsable);
            }
        }

        public void UpdateItemTypeCollectQuest(IEnumerable<ItemBase> items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items.OrderBy(i => i.Id))
            {
                var targets = _quests
                    .OfType<ItemTypeCollectQuest>()
                    .Where(i => i.ItemType == item.ItemType && !i.Complete);
                foreach (var target in targets)
                {
                    target.Update(item);
                }
            }
        }

        public IValue Serialize()
        {
            if (_listVersion > 1)
            {
                return Dictionary.Empty
                    .SetItem(_listVersionKey, _listVersion.Serialize())
                    .SetItem(_questsKey, (IValue) new List(_quests
                        .OrderBy(i => i.Id)
                        .Select(q => q.Serialize())))
                    .SetItem(_completedQuestIdsKey, (IValue) new List(completedQuestIds
                        .OrderBy(i => i)
                        .Select(i => i.Serialize())));
            }

            return new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) _questsKeyDeprecated] = new List(_quests
                    .OrderBy(i => i.Id)
                    .Select(q => q.Serialize())),
                [(Text) _completedQuestIdsKeyDeprecated] = new List(completedQuestIds
                    .OrderBy(i => i)
                    .Select(i => i.Serialize()))
            });
        }

        public void UpdateCombinationEquipmentQuest(int recipeId)
        {
            var targets = _quests.OfType<CombinationEquipmentQuest>()
                .Where(q => !q.Complete);
            foreach (var target in targets)
            {
                target.Update(recipeId);
            }
        }


        public CollectionMap UpdateCompletedQuest(CollectionMap eventMap)
        {
            const QuestEventType type = QuestEventType.Complete;
            eventMap[(int) type] = _quests.Count(i => i.Complete);
            return UpdateGeneralQuest(new[] {type}, eventMap);
        }


        private static QuestReward GetQuestReward(
            int rewardId,
            QuestRewardSheet rewardSheet,
            QuestItemRewardSheet itemRewardSheet)
        {
            var itemMap = new Dictionary<int, int>();
            if (rewardSheet.TryGetValue(rewardId, out var questRewardRow))
            {
                foreach (var id in questRewardRow.RewardIds.OrderBy(i => i))
                {
                    if (itemRewardSheet.TryGetValue(id, out var itemRewardRow))
                    {
                        itemMap[itemRewardRow.ItemId] = itemRewardRow.Count;
                    }
                }
            }

            return new QuestReward(itemMap);
        }
    }
}
