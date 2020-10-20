using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Quest
{
    [Serializable]
    public class GeneralQuest : Quest
    {
        public readonly QuestEventType Event;

        public GeneralQuest(GeneralQuestSheet.Row data, QuestReward reward)
            : base(data, reward)
        {
            Event = data.Event;
        }

        public GeneralQuest(Dictionary serialized) : base(serialized)
        {
            Event = (QuestEventType)(int)((Integer)serialized["event"]).Value;
        }

        public override QuestType QuestType
        {
            get
            {
                switch (Event)
                {
                    case QuestEventType.Create:
                    case QuestEventType.Level:
                    case QuestEventType.Die:
                    case QuestEventType.Complete:
                        return QuestType.Adventure;
                    case QuestEventType.Enhancement:
                    case QuestEventType.Equipment:
                    case QuestEventType.Consumable:
                        return QuestType.Craft;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override void Check()
        {
            if (Complete)
                return;

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

        protected override string TypeId => "generalQuest";

        public void Update(CollectionMap eventMap)
        {
            if (Complete)
                return;

            var key = (int)Event;
            eventMap.TryGetValue(key, out _current);
            Check();
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"event"] = (Integer)(int)Event,
            }.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002

    }
}
