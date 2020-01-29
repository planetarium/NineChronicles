using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.EnumType;
using Nekoyume.TableData;

namespace Nekoyume.Model.Quest
{
    [Serializable]
    public class TradeQuest : Quest
    {
        public override QuestType QuestType => QuestType.Exchange;
        public readonly TradeType Type;

        public TradeQuest(TradeQuestSheet.Row data) : base(data)
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

        public override string GetName()
        {
            var format = LocalizationManager.Localize("QUEST_TRADE_CURRENT_INFO_FORMAT");
            return string.Format(format, Type.GetLocalizedString());
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
