using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.State;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.Game.Quest
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
        public bool isLocalCompleted;

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

        protected int Goal { get; }

        public int Id { get; }

        public QuestReward Reward { get; }

        public bool Receive { get; set; }

        public float Progress => (float) _current / Goal;

        public const string GoalFormat = "({0}/{1})";

        protected Quest(QuestSheet.Row data)
        {
            Id = data.Id;
            Goal = data.Goal;
            var itemMap = new Dictionary<int, int>();
            if (Game.instance.TableSheets.QuestRewardSheet.TryGetValue(data.QuestRewardId, out var questRewardRow))
            {
                foreach (var rewardId in questRewardRow.RewardIds)
                {
                    if (Game.instance.TableSheets.QuestItemRewardSheet.TryGetValue(rewardId, out var itemRewardRow))
                    {
                        itemMap[itemRewardRow.ItemId] = itemRewardRow.Count;
                    }
                }
            }

            Reward = new QuestReward(itemMap);
        }

        public abstract void Check();
        public abstract string GetName();
        protected abstract string TypeId { get; }

        protected Quest(Bencodex.Types.Dictionary serialized)
        {
            Complete = ((Bencodex.Types.Boolean) serialized["complete"]).Value;
            Goal = (int) ((Integer) serialized["goal"]).Value;
            _current = (int) ((Integer) serialized["current"]).Value;
            Id = (int) ((Integer) serialized["id"]).Value;
            Reward = new QuestReward((Dictionary) serialized["reward"]);
            serialized.TryGetValue((Text) "receive", out var receive);
            Receive = ((Bencodex.Types.Boolean?) receive)?.Value ?? false;
        }

        public abstract string GetProgressText();

        public virtual IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Bencodex.Types.Text) "typeId"] = (Bencodex.Types.Text) TypeId,
                [(Bencodex.Types.Text) "complete"] = new Bencodex.Types.Boolean(Complete),
                [(Text) "goal"] = (Integer) Goal,
                [(Text) "current"] = (Integer) _current,
                [(Text) "id"] = (Integer) Id,
                [(Text) "reward"] = Reward.Serialize(),
                [(Bencodex.Types.Text) "receive"] = new Bencodex.Types.Boolean(Receive),
            });

        public static Quest Deserialize(Bencodex.Types.Dictionary serialized)
        {
            string typeId = ((Text) serialized["typeId"]).Value;
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
                Debug.LogErrorFormat("{0} was raised during deserialize: {1}", e.GetType().FullName, serialized);
                throw;
            }
        }
    }

    [Serializable]
    public class QuestList : IEnumerable<Quest>, IState
    {
        private readonly List<Quest> quests;

        public QuestList(QuestSheet sheet)
        {
            quests = new List<Quest>();
            if (sheet is null)
                return;
            
            foreach (var questData in sheet.OrderedList)
            {
                Quest quest;
                switch (questData)
                {
                    case CollectQuestSheet.Row row:
                        quest = new CollectQuest(row);
                        quests.Add(quest);
                        break;
                    case CombinationQuestSheet.Row row1:
                        quest = new CombinationQuest(row1);
                        quests.Add(quest);
                        break;
                    case GeneralQuestSheet.Row row2:
                        quest = new GeneralQuest(row2);
                        quests.Add(quest);
                        break;
                    case ItemEnhancementQuestSheet.Row row3:
                        quest = new ItemEnhancementQuest(row3);
                        quests.Add(quest);
                        break;
                    case ItemGradeQuestSheet.Row row4:
                        quest = new ItemGradeQuest(row4);
                        quests.Add(quest);
                        break;
                    case MonsterQuestSheet.Row row5:
                        quest = new MonsterQuest(row5);
                        quests.Add(quest);
                        break;
                    case TradeQuestSheet.Row row6:
                        quest = new TradeQuest(row6);
                        quests.Add(quest);
                        break;
                    case WorldQuestSheet.Row row7:
                        quest = new WorldQuest(row7);
                        quests.Add(quest);
                        break;
                    case ItemTypeCollectQuestSheet.Row row8:
                        quest = new ItemTypeCollectQuest(row8);
                        quests.Add(quest);
                        break;
                    case GoldQuestSheet.Row row9:
                        quest = new GoldQuest(row9);
                        quests.Add(quest);
                        break;
                }
            }
        }


        public QuestList(Bencodex.Types.List serialized)
        {
            quests = serialized.Select(q => Quest.Deserialize((Bencodex.Types.Dictionary) q))
                .ToList();
        }

        public IEnumerator<Quest> GetEnumerator()
        {
            return quests.GetEnumerator();
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
            var targets = quests.OfType<CombinationQuest>()
                .Where(i => i.ItemType == itemUsable.Data.ItemType &&
                            i.ItemSubType == itemUsable.Data.ItemSubType &&
                            !i.Complete);
            foreach (var target in targets)
            {
                target.Update(new List<ItemBase> {itemUsable});
            }
        }

        public void UpdateTradeQuest(TradeType type, decimal price)
        {
//            var quest = quests.OfType<TradeQuest>()
//                .FirstOrDefault(i => i.Type == type && !i.Complete);
//            quest?.Check();
            var tradeQuests = quests.OfType<TradeQuest>()
                .Where(i => i.Type == type && !i.Complete);
            foreach (var tradeQuest in tradeQuests)
            {
                tradeQuest.Check();
            }

//            var goldQuest = quests.OfType<GoldQuest>()
//                .FirstOrDefault(i => i.Type == type && !i.Complete);
//            goldQuest?.Update(price);
            var goldQuests = quests.OfType<GoldQuest>()
                .Where(i => i.Type == type && !i.Complete);
            foreach (var goldQuest in goldQuests)
            {
                goldQuest.Update(price);
            }
        }

        public void UpdateStageQuest(CollectionMap stageMap)
        {
            var stageQuests = quests.OfType<WorldQuest>().ToList();
            foreach (var quest in stageQuests)
            {
                quest.Update(stageMap);
            }
        }

        public void UpdateMonsterQuest(CollectionMap monsterMap)
        {
            var monsterQuests = quests.OfType<MonsterQuest>().ToList();
            foreach (var quest in monsterQuests)
            {
                quest.Update(monsterMap);
            }
        }

        public void UpdateCollectQuest(CollectionMap itemMap)
        {
            var collectQuests = quests.OfType<CollectQuest>().ToList();
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
            var targets = quests.OfType<ItemEnhancementQuest>()
                .Where(i => !i.Complete && i.Grade == equipment.Data.Grade);
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
                var targets = quests.OfType<GeneralQuest>()
                    .Where(i => i.Event == type && !i.Complete);
                foreach (var target in targets)
                {
                    target.Update(eventMap);
                }
            }

            return eventMap;
        }

        public CollectionMap UpdateCompletedQuest(CollectionMap eventMap)
        {
            const QuestEventType type = QuestEventType.Complete;
            eventMap[(int) type] = quests.Count(i => i.Complete);
            return UpdateGeneralQuest(new[] {type}, eventMap);
        }

        public IValue Serialize() =>
            new Bencodex.Types.List(this.Select(q => q.Serialize()));

        public void UpdateItemGradeQuest(ItemUsable itemUsable)
        {
//            var quest = quests.OfType<ItemGradeQuest>()
//                .FirstOrDefault(i => i.Grade == itemUsable.Data.Grade && !i.Complete);
//            quest?.Update(itemUsable);
            var targets = quests.OfType<ItemGradeQuest>()
                .Where(i => i.Grade == itemUsable.Data.Grade && !i.Complete);
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
                var targets = quests.OfType<ItemTypeCollectQuest>()
                    .Where(i => i.ItemType == item.Data.ItemType && !i.Complete);
                foreach (var target in targets)
                {
                    target.Update(item);
                }
            }
        }
    }
}
