using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Data;
using Nekoyume.Game.Item;
using Nekoyume.Model;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public abstract class Quest
    {
        protected Quest(Data.Table.Quest data)
        {
            goal = data.goal;
            reward = data.reward;
        }

        public int goal;
        public decimal reward;
        public bool Complete { get; protected set; }

        public abstract void Check(Player player, List<ItemBase> items);
        public abstract string ToInfo();
    }

    [Serializable]
    public class QuestList : IEnumerable<Quest>
    {
        public QuestList()
        {
            quests = new List<Quest>();
            foreach (var data in Tables.instance.Quest.Values)
            {
                var quest = new BattleQuest(data);
                quests.Add(quest);
            }

            foreach (var collectData in Tables.instance.CollectQuest.Values)
            {
                var quest = new CollectQuest(collectData);
                quests.Add(quest);
            }

            foreach (var combinationData in Tables.instance.CombinationQuest.Values)
            {
                var quest = new CombinationQuest(combinationData);
                quests.Add(quest);
            }

            foreach (var tradeQuestData in Tables.instance.TradeQuest.Values)
            {
                var quest = new TradeQuest(tradeQuestData);
                quests.Add(quest);
            }
        }

        private readonly List<Quest> quests;

        public void UpdateStageQuest(Player player, List<ItemBase> items)
        {
            var questList = quests.Where(q => q is BattleQuest || q is CollectQuest).ToArray();
            foreach (var quest in questList)
            {
                quest.Check(player, items);
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
                .FirstOrDefault(i => i.cls == itemUsable.Data.cls && !i.Complete);
            quest?.Check(null, new List<ItemBase> {itemUsable});
        }

        public void UpdateTradeQuest(string type)
        {
            var quest = quests.OfType<TradeQuest>()
                .FirstOrDefault(i => i.type == type && !i.Complete);
            quest?.Check(null, null);
        }
    }
}
