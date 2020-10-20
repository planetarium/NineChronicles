using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Quest
{
    [Serializable]
    public class CollectQuest : Quest
    {
        public override QuestType QuestType => QuestType.Obtain;

        public readonly int ItemId;

        public CollectQuest(CollectQuestSheet.Row data, QuestReward reward) 
            : base(data, reward)
        {
            ItemId = data.ItemId;
        }

        public CollectQuest(Dictionary serialized) : base(serialized)
        {
            ItemId = (int)((Integer)serialized["itemId"]).Value;
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

        protected override string TypeId => "collectQuest";

        public void Update(CollectionMap itemMap)
        {
            if (Complete)
                return;

            itemMap.TryGetValue(ItemId, out _current);
            Check();
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"itemId"] = (Integer)ItemId,
            }.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002
    }
}
