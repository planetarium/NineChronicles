using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.EnumType;
using Nekoyume.TableData;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class TradeQuest : Quest
    {
        private int _current;
        public override QuestType QuestType => QuestType.Exchange;
        public readonly TradeType Type;

        public TradeQuest(TradeQuestSheet.Row data) : base(data)
        {
            Type = data.Type;
        }

        public TradeQuest(Dictionary serialized) : base(serialized)
        {
            _current = (int) ((Integer) serialized[(Bencodex.Types.Text) "current"]).Value;
            Type = (TradeType) (int) ((Integer) serialized[(Bencodex.Types.Text) "type"]).Value;
        }

        public override void Check()
        {
            if (Complete)
                return;
            _current += 1;
            Complete = _current >= Goal;
        }

        public override string ToInfo()
        {
            return string.Format(GoalFormat, GetName(), _current, Goal);
        }

        public override string GetName()
        {
            var format = LocalizationManager.Localize("QUEST_COLLECT_CURRENT_INFO_FORMAT");
            return string.Format(format, Type.GetLocalizedString());
        }

        protected override string TypeId => "tradeQuest";
        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "current"] = (Integer) _current,
                [(Text) "type"] = (Integer) (int) Type,
            }.Union((Dictionary) base.Serialize()));

    }
}
