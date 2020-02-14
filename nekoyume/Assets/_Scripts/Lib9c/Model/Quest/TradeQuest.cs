using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.EnumType;
using Nekoyume.TableData;

namespace Nekoyume.Model.Quest
{
    [Serializable]
    public class TradeQuest : Quest
    {
        public override QuestType QuestType => QuestType.Exchange;
        public readonly TradeType Type;

        public TradeQuest(TradeQuestSheet.Row data, QuestReward reward) 
            : base(data, reward)
        {
            Type = data.Type;
        }

        public TradeQuest(Dictionary serialized) : base(serialized)
        {
            Type = (TradeType)(int)((Integer)serialized["type"]).Value;
        }

        public override void Check()
        {
            if (Complete)
                return;

            _current += 1;
            Complete = _current >= Goal;
        }

        public override string GetProgressText()
        {
            return string.Format(GoalFormat, Math.Min(Goal, _current), Goal);
        }

        protected override string TypeId => "tradeQuest";
        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"type"] = (Integer)(int)Type,
            }.Union((Dictionary)base.Serialize()));

    }
}
