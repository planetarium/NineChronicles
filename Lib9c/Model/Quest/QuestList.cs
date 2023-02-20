using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex.Types;
using Libplanet.Assets;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.Quest
{
    #region Exceptions

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

    #endregion

    [Serializable]
    public class QuestList : IEnumerable<Quest>, IState
    {
        public const string QuestsKey = "q";
        private readonly List<Quest> _quests;

        // FIXME: Consider removing the `_listVersion` field.
        public const string ListVersionKey = "v";
        private int _listVersion = 1;
        public int ListVersion => _listVersion;

        public const string CompletedQuestIdsKey = "c";
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
                var reward = GetQuestReward(
                    questData.QuestRewardId,
                    questRewardSheet,
                    questItemRewardSheet
                );

                var quest = CreateQuest(questData, reward, equipmentItemRecipeSheet);
                if (quest is null)
                {
                    continue;
                }

                _quests.Add(quest);
            }
        }


        public QuestList(Dictionary serialized)
        {
            _listVersion = serialized.TryGetValue((Text) ListVersionKey, out var listVersion)
                ? listVersion.ToInteger()
                : 1;

            if (_listVersion == 1)
            {
                _quests = serialized.TryGetValue((Text) QuestsKeyDeprecated, out var questsValue)
                    ? questsValue.ToList(Quest.Deserialize)
                    : new List<Quest>();

                completedQuestIds = serialized.TryGetValue((Text) CompletedQuestIdsKeyDeprecated, out var idsValue)
                    ? idsValue.ToList(StateExtensions.ToInteger)
                    : new List<int>();
            }
            else
            {
                _quests = serialized.TryGetValue((Text) QuestsKey, out var q)
                    ? q.ToList(Quest.Deserialize)
                    : new List<Quest>();

                completedQuestIds = serialized.TryGetValue((Text) CompletedQuestIdsKey, out var cqi)
                    ? cqi.ToList(StateExtensions.ToInteger)
                    : new List<int>();
            }
        }

        public void UpdateList(
            QuestSheet questSheet,
            QuestRewardSheet questRewardSheet,
            QuestItemRewardSheet questItemRewardSheet,
            EquipmentItemRecipeSheet equipmentItemRecipeSheet)
        {
            UpdateListV1(
                _listVersion + 1,
                questSheet,
                questRewardSheet,
                questItemRewardSheet,
                equipmentItemRecipeSheet);
        }

        /// <exception cref="UpdateListVersionException"></exception>
        /// <exception cref="UpdateListQuestsCountException"></exception>
        /// <exception cref="Exception"></exception>
        [Obsolete("Use UpdateList()")]
        public void UpdateListV1(
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

            ImmutableHashSet<int> questIds = _quests.Select(q => q.Id).ToImmutableHashSet();
            foreach (var questRow in questSheet.OrderedList)
            {
                if (questIds.Contains(questRow.Id))
                {
                    continue;
                }

                var reward = GetQuestReward(
                    questRow.QuestRewardId,
                    questRewardSheet,
                    questItemRewardSheet);

                Quest quest = CreateQuest(questRow, reward, equipmentItemRecipeSheet);
                if (quest is null)
                {
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

        public CollectionMap UpdateGeneralQuest(
            IEnumerable<QuestEventType> types,
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
                    .SetItem(ListVersionKey, _listVersion.Serialize())
                    .SetItem(QuestsKey, (IValue) new List(_quests
                        .OrderBy(i => i.Id)
                        .Select(q => q.Serialize())))
                    .SetItem(CompletedQuestIdsKey, (IValue) new List(completedQuestIds
                        .OrderBy(i => i)
                        .Select(i => i.Serialize())));
            }

            return new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) QuestsKeyDeprecated] = new List(_quests
                    .OrderBy(i => i.Id)
                    .Select(q => q.Serialize())),
                [(Text) CompletedQuestIdsKeyDeprecated] = new List(completedQuestIds
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

        private static Quest CreateQuest(
            QuestSheet.Row row,
            QuestReward reward,
            EquipmentItemRecipeSheet equipmentItemRecipeSheet)
        {
            Quest quest = default;
            switch (row)
            {
                case CollectQuestSheet.Row r:
                    quest = new CollectQuest(r, reward);
                    break;
                case CombinationQuestSheet.Row r:
                    quest = new CombinationQuest(r, reward);
                    break;
                case GeneralQuestSheet.Row r:
                    quest = new GeneralQuest(r, reward);
                    break;
                case ItemEnhancementQuestSheet.Row r:
                    quest = new ItemEnhancementQuest(r, reward);
                    break;
                case ItemGradeQuestSheet.Row r:
                    quest = new ItemGradeQuest(r, reward);
                    break;
                case MonsterQuestSheet.Row r:
                    quest = new MonsterQuest(r, reward);
                    break;
                case TradeQuestSheet.Row r:
                    quest = new TradeQuest(r, reward);
                    break;
                case WorldQuestSheet.Row r:
                    quest = new WorldQuest(r, reward);
                    break;
                case ItemTypeCollectQuestSheet.Row r:
                    quest = new ItemTypeCollectQuest(r, reward);
                    break;
                case GoldQuestSheet.Row r:
                    quest = new GoldQuest(r, reward);
                    break;
                case CombinationEquipmentQuestSheet.Row r:
                    int stageId;
                    var recipeRow = equipmentItemRecipeSheet.Values
                        .FirstOrDefault(e => e.Id == r.RecipeId);
                    if (recipeRow is null)
                    {
                        throw new ArgumentException($"Invalid Recipe Id : {r.RecipeId}");
                    }

                    stageId = recipeRow.UnlockStage;
                    quest = new CombinationEquipmentQuest(r, reward, stageId);
                    break;
            }

            return quest;
        }
    }
}
