using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Quest
{
    [Serializable]
    public class ItemGradeQuest : Quest
    {
        public readonly int Grade;
        private readonly List<int> _itemIds = new List<int>();

        public ItemGradeQuest(ItemGradeQuestSheet.Row data, QuestReward reward) 
            : base(data, reward)
        {
            Grade = data.Grade;
        }

        public ItemGradeQuest(Dictionary serialized) : base(serialized)
        {
            Grade = (int)((Integer)serialized["grade"]).Value;
            _itemIds = serialized["itemIds"].ToList(i => (int)((Integer)i).Value);
        }

        public override QuestType QuestType => QuestType.Obtain;

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

        public void Update(ItemUsable itemUsable)
        {
            if (Complete)
                return;

            if (!_itemIds.Contains(itemUsable.Data.Id))
            {
                _current++;
                _itemIds.Add(itemUsable.Data.Id);
            }
            Check();
        }

        protected override string TypeId => "itemGradeQuest";

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"grade"] = (Integer)Grade,
                [(Text)"itemIds"] = (List)_itemIds.Select(i => (Integer)i).Serialize(),
            }.Union((Dictionary)base.Serialize()));

    }
}
