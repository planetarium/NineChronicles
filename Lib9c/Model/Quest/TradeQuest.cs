using System;
using System.Collections.Generic;
using System.Globalization;
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

        public TradeType Type
        {
            get
            {
                if (_serializedType is { })
                {
                    _type = (TradeType) (int) _serializedType;
                    _serializedType = null;
                }

                return _type;
            }
        }
        private TradeType _type;
        private Integer? _serializedType;

        public TradeQuest(TradeQuestSheet.Row data, QuestReward reward)
            : base(data, reward)
        {
            _type = data.Type;
        }

        public TradeQuest(Dictionary serialized) : base(serialized)
        {
            _serializedType = (Integer) serialized["type"];
        }

        public override void Check()
        {
            if (Complete)
                return;

            _current += 1;
            Complete = _current >= Goal;
        }

        // FIXME: 이 메서드 구현은 중복된 코드가 다른 데서도 많이 있는 듯.
        public override string GetProgressText() =>
            string.Format(
                CultureInfo.InvariantCulture,
                GoalFormat,
                Math.Min(Goal, _current),
                Goal
            );

        protected override string TypeId => "tradeQuest";
        public override IValue Serialize() =>
            ((Dictionary) base.Serialize())
            .Add("type", _serializedType ?? (int) Type);
    }
}
