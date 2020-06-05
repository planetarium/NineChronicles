using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Model.Quest
{
    public enum QuestType
    {
        Adventure,
        Obtain,
        Craft,
        Exchange
    }

    [Serializable]
    public abstract class Quest : IState
    {
        [NonSerialized]
        public bool isReceivable = false;

        protected int _current;

        public abstract QuestType QuestType { get; }

        private static readonly Dictionary<string, Func<Dictionary, Quest>> Deserializers =
            new Dictionary<string, Func<Dictionary, Quest>>
            {
                ["collectQuest"] = d => new CollectQuest(d),
                ["combinationQuest"] = d => new CombinationQuest(d),
                ["monsterQuest"] = d => new MonsterQuest(d),
                ["tradeQuest"] = d => new TradeQuest(d),
                ["worldQuest"] = d => new WorldQuest(d),
                ["itemEnhancementQuest"] = d => new ItemEnhancementQuest(d),
                ["generalQuest"] = d => new GeneralQuest(d),
                ["itemGradeQuest"] = d => new ItemGradeQuest(d),
                ["itemTypeCollectQuest"] = d => new ItemTypeCollectQuest(d),
                ["GoldQuest"] = d => new GoldQuest(d),
            };

        public bool Complete { get; protected set; }

        public int Goal { get; }

        public int Id { get; }

        public QuestReward Reward { get; }

        /// <summary>
        /// 이미 퀘스트 보상이 액션에서 지급되었는가?
        /// </summary>
        public bool IsPaidInAction { get; set; }

        public float Progress => (float)_current / Goal;

        public const string GoalFormat = "({0}/{1})";

        protected Quest(QuestSheet.Row data, QuestReward reward)
        {
            Id = data.Id;
            Goal = data.Goal;
            Reward = reward;
        }

        public abstract void Check();
        protected abstract string TypeId { get; }

        protected Quest(Dictionary serialized)
        {
            Complete = ((Bencodex.Types.Boolean)serialized["complete"]).Value;
            Goal = (int)((Integer)serialized["goal"]).Value;
            _current = (int)((Integer)serialized["current"]).Value;
            Id = (int)((Integer)serialized["id"]).Value;
            Reward = new QuestReward((Dictionary)serialized["reward"]);
            IsPaidInAction = serialized["isPaidInAction"].ToNullableBoolean() ?? false;
        }

        public abstract string GetProgressText();

        public virtual IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"typeId"] = (Text)TypeId,
                [(Text)"complete"] = new Bencodex.Types.Boolean(Complete),
                [(Text)"goal"] = (Integer)Goal,
                [(Text)"current"] = (Integer)_current,
                [(Text)"id"] = (Integer)Id,
                [(Text)"reward"] = Reward.Serialize(),
                [(Text)"isPaidInAction"] = new Bencodex.Types.Boolean(IsPaidInAction),
            });

        public static Quest Deserialize(Dictionary serialized)
        {
            string typeId = ((Text)serialized["typeId"]).Value;
            Func<Dictionary, Quest> deserializer;
            try
            {
                deserializer = Deserializers[typeId];
            }
            catch (KeyNotFoundException)
            {
                string typeIds = string.Join(
                    ", ",
                    Deserializers.Keys.OrderBy(k => k, StringComparer.InvariantCulture)
                );
                throw new ArgumentException(
                    $"Unregistered typeId: {typeId}; available typeIds: {typeIds}"
                );
            }

            try
            {
                return deserializer(serialized);
            }
            catch (Exception e)
            {
                Log.Error(e, "{0} was raised during deserialize: {1}", e.GetType().FullName, serialized);
                throw;
            }
        }

        public static Quest Deserialize(IValue arg)
        {
            return Deserialize((Dictionary) arg);
        }
    }

    [Serializable]
    public class QuestList : IEnumerable<Quest>, IState
    {
        private readonly List<Quest> _quests;
        public List<int> completedQuestIds = new List<int>();

        public QuestList(
            QuestSheet questSheet,
            QuestRewardSheet questRewardSheet,
            QuestItemRewardSheet questItemRewardSheet)
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
                }
            }
        }


        public QuestList(Dictionary serialized)
        {
            _quests = serialized.TryGetValue((Text)"quests", out var questsValue)
                ? questsValue.ToList(Quest.Deserialize)
                : new List<Quest>();

            completedQuestIds = serialized.TryGetValue((Text) "completedQuestIds", out var idsValue)
                ? idsValue.ToList(StateExtensions.ToInteger)
                : new List<int>();
        }

        public IEnumerator<Quest> GetEnumerator()
        {
            return _quests.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void UpdateCombinationQuest(ItemUsable itemUsable)
        {
            //            var quest = quests.OfType<CombinationQuest>()
            //                .FirstOrDefault(i => i.ItemType == itemUsable.Data.ItemType &&
            //                                     i.ItemSubType == itemUsable.Data.ItemSubType &&
            //                                     !i.Complete);
            //            quest?.Update(new List<ItemBase> {itemUsable});
            var targets = _quests.OfType<CombinationQuest>()
                .Where(i => i.ItemType == itemUsable.ItemType &&
                            i.ItemSubType == itemUsable.ItemSubType &&
                            !i.Complete);
            foreach (var target in targets)
            {
                target.Update(new List<ItemBase> { itemUsable });
            }
        }

        public void UpdateTradeQuest(TradeType type, decimal price)
        {
            //            var quest = quests.OfType<TradeQuest>()
            //                .FirstOrDefault(i => i.Type == type && !i.Complete);
            //            quest?.Check();
            var tradeQuests = _quests.OfType<TradeQuest>()
                .Where(i => i.Type == type && !i.Complete);
            foreach (var tradeQuest in tradeQuests)
            {
                tradeQuest.Check();
            }

            //            var goldQuest = quests.OfType<GoldQuest>()
            //                .FirstOrDefault(i => i.Type == type && !i.Complete);
            //            goldQuest?.Update(price);
            var goldQuests = _quests.OfType<GoldQuest>()
                .Where(i => i.Type == type && !i.Complete);
            foreach (var goldQuest in goldQuests)
            {
                goldQuest.Update(price);
            }
        }

        public void UpdateStageQuest(CollectionMap stageMap)
        {
            var stageQuests = _quests.OfType<WorldQuest>().ToList();
            foreach (var quest in stageQuests)
            {
                quest.Update(stageMap);
            }
        }

        public void UpdateMonsterQuest(CollectionMap monsterMap)
        {
            var monsterQuests = _quests.OfType<MonsterQuest>().ToList();
            foreach (var quest in monsterQuests)
            {
                quest.Update(monsterMap);
            }
        }

        public void UpdateCollectQuest(CollectionMap itemMap)
        {
            var collectQuests = _quests.OfType<CollectQuest>().ToList();
            foreach (var quest in collectQuests)
            {
                quest.Update(itemMap);
            }
        }

        public void UpdateItemEnhancementQuest(Equipment equipment)
        {
            //            var quest = quests.OfType<ItemEnhancementQuest>()
            //                .FirstOrDefault(i => !i.Complete && i.Grade == equipment.Data.Grade);
            //            quest?.Update(equipment);
            var targets = _quests.OfType<ItemEnhancementQuest>()
                .Where(i => !i.Complete && i.Grade == equipment.Grade);
            foreach (var target in targets)
            {
                target.Update(equipment);
            }
        }

        public CollectionMap UpdateGeneralQuest(IEnumerable<QuestEventType> types, CollectionMap eventMap)
        {
            foreach (var type in types)
            {
                //                var quest = quests.OfType<GeneralQuest>()
                //                    .FirstOrDefault(i => i.Event == type && !i.Complete);
                //                quest?.Update(eventMap);
                var targets = _quests.OfType<GeneralQuest>()
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
            //            var quest = quests.OfType<ItemGradeQuest>()
            //                .FirstOrDefault(i => i.Grade == itemUsable.Data.Grade && !i.Complete);
            //            quest?.Update(itemUsable);
            var targets = _quests.OfType<ItemGradeQuest>()
                .Where(i => i.Grade == itemUsable.Grade && !i.Complete);
            foreach (var target in targets)
            {
                target.Update(itemUsable);
            }
        }

        public void UpdateItemTypeCollectQuest(IEnumerable<ItemBase> items)
        {
            foreach (var item in items)
            {
                //                var quest = quests.OfType<ItemTypeCollectQuest>()
                //                    .FirstOrDefault(i => i.ItemType == item.Data.ItemType && !i.Complete);
                //                quest?.Update(item);
                var targets = _quests.OfType<ItemTypeCollectQuest>()
                    .Where(i => i.ItemType == item.ItemType && !i.Complete);
                foreach (var target in targets)
                {
                    target.Update(item);
                }
            }
        }

        public IValue Serialize() => new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"quests"] = new List(this.Select(q => q.Serialize())),
                [(Text) "completedQuestIds"] = new List(completedQuestIds.Select(i => i.Serialize()))
            });

        public CollectionMap UpdateCompletedQuest(CollectionMap eventMap)
        {
            const QuestEventType type = QuestEventType.Complete;
            eventMap[(int)type] = _quests.Count(i => i.Complete);
            return UpdateGeneralQuest(new[] { type }, eventMap);
        }


        private static QuestReward GetQuestReward(
            int rewardId,
            QuestRewardSheet rewardSheet,
            QuestItemRewardSheet itemRewardSheet)
        {
            var itemMap = new Dictionary<int, int>();
            if (rewardSheet.TryGetValue(rewardId, out var questRewardRow))
            {
                foreach (var id in questRewardRow.RewardIds)
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
