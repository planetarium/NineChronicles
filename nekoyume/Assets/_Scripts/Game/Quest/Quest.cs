using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Battle;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.State;
using Nekoyume.TableData;

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
    public abstract class Quest
    {
        public virtual QuestType QuestType { get; }
        public QuestSheet.Row Data { get; }
        public bool Complete { get; protected set; }
        
        protected Quest(QuestSheet.Row data)
        {
            Data = data;
        }

        public abstract void Check();
        public abstract string ToInfo();

        public Quest Copy(bool complete)
        {
            Quest clone = (Quest) MemberwiseClone();
            clone.Complete = complete;
            return clone;
        }
    }

    [Serializable]
    public class QuestList : IEnumerable<Quest>, IState
    {
        private readonly List<Quest> quests;
        
        public QuestList()
        {
            quests = new List<Quest>();
            foreach (var data in Game.instance.TableSheets.WorldQuestSheet.OrderedList)
            {
                var quest = new WorldQuest(data);
                quests.Add(quest);
            }

            foreach (var collectData in Game.instance.TableSheets.CollectQuestSheet.OrderedList)
            {
                var quest = new CollectQuest(collectData);
                quests.Add(quest);
            }

            foreach (var combinationData in Game.instance.TableSheets.CombinationQuestSheet.OrderedList)
            {
                var quest = new CombinationQuest(combinationData);
                quests.Add(quest);
            }

            foreach (var tradeQuestData in Game.instance.TableSheets.TradeQuestSheet.OrderedList)
            {
                var quest = new TradeQuest(tradeQuestData);
                quests.Add(quest);
            }
        }

        public QuestList(Bencodex.Types.List serialized) : this()
        {
            ImmutableHashSet<QuestSheet.Row> completedQuests = serialized
                .Select(q => QuestSheet.Row.Deserialize((Bencodex.Types.Dictionary) q))
                .ToImmutableHashSet();
            quests = quests
                .Select(q => completedQuests.Contains(q.Data) ? q.Copy(true) : q)
                .ToList();
        }

        public void UpdateStageQuest(Simulator simulator)
        {
            foreach (var quest in quests)
            {
                switch (quest)
                {
                    case CollectQuest cq:
                        cq.Update(simulator.rewards);
                        break;
                }
            }
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
            var quest = quests.OfType<CombinationQuest>()
                .FirstOrDefault(i => i.Data.ItemType == itemUsable.Data.ItemType &&
                                     i.Data.ItemSubType == itemUsable.Data.ItemSubType &&
                                     !i.Complete);
            quest?.Update(new List<ItemBase> {itemUsable});
        }

        public void UpdateTradeQuest(TradeType type)
        {
            var quest = quests.OfType<TradeQuest>()
                .FirstOrDefault(i => i.Data.Type == type && !i.Complete);
            quest?.Check();
        }

        public void UpdateStageQuest(CollectionMap stageMap)
        {
            var stageQuests = quests.OfType<WorldQuest>().ToList();
            foreach (var quest in stageQuests)
            {
                quest.Update(stageMap);
            }
        }

        public IValue Serialize() =>
            new Bencodex.Types.List(this.Where(q => q.Complete).Select(q => q.Data.Serialize()));
    }
}
