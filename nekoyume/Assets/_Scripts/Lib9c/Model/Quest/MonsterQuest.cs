using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Quest
{
    [Serializable]
    public class MonsterQuest : Quest
    {
        public readonly int MonsterId;

        public MonsterQuest(MonsterQuestSheet.Row data, QuestReward reward) 
            : base(data, reward)
        {
            MonsterId = data.MonsterId;
        }

        public MonsterQuest(Dictionary serialized) : base(serialized)
        {
            MonsterId = (int)((Integer)serialized["monsterId"]).Value;
        }

        public override QuestType QuestType => QuestType.Adventure;

        public override void Check()
        {
            if (Complete)
                return;

            Complete = _current >= Goal;
        }

        public override string GetProgressText()
        {
            return string.Format(GoalFormat, Math.Min(Goal, _current), Goal);
        }

        protected override string TypeId => "monsterQuest";

        public void Update(CollectionMap monsterMap)
        {
            if (Complete)
                return;

            monsterMap.TryGetValue(MonsterId, out _current);
            Check();
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"monsterId"] = (Integer)MonsterId,
            }.Union((Dictionary)base.Serialize()));
    }
}
