using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.EnumType;
using Nekoyume.TableData;

namespace Nekoyume.Model.Quest
{
    [Serializable]
    public class GoldQuest : Quest
    {
        public readonly TradeType Type;

        public GoldQuest(GoldQuestSheet.Row data, QuestReward reward) 
            : base(data, reward)
        {
            Type = data.Type;
        }

        public GoldQuest(Dictionary serialized) : base(serialized)
        {
            Type = (TradeType)(int)((Integer)serialized["type"]).Value;
        }

        public override QuestType QuestType => QuestType.Exchange;

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

        protected override string TypeId => "GoldQuest";

        public void Update(decimal gold)
        {
            if (Complete)
                return;

            _current += (int)gold;
            Check();
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"type"] = (Integer)(int)Type,
            }.Union((Dictionary)base.Serialize()));

    }
}
