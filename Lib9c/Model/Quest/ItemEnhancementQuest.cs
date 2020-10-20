using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace Nekoyume.Model.Quest
{
    [Serializable]
    public class ItemEnhancementQuest : Quest
    {
        public readonly int Grade;
        private readonly int _count;
        public int Count => _count;
        public override float Progress => (float) _current / _count;

        public ItemEnhancementQuest(ItemEnhancementQuestSheet.Row data, QuestReward reward) 
            : base(data, reward)
        {
            _count = data.Count;
            Grade = data.Grade;
        }

        public ItemEnhancementQuest(Dictionary serialized) : base(serialized)
        {
            Grade = (int)((Integer)serialized["grade"]).Value;
            _count = (int)((Integer)serialized["count"]).Value;
        }

        public override QuestType QuestType => QuestType.Craft;

        public override void Check()
        {
            if (Complete)
                return;

            Complete = _count == _current;
        }

        public override string GetProgressText() =>
            string.Format(
                CultureInfo.InvariantCulture,
                GoalFormat,
                Math.Min(_count, _current),
               _count
            );

        public void Update(Equipment equipment)
        {
            if (Complete)
                return;

            if (equipment.level == Goal)
            {
                _current++;
            }

            Check();
        }

        protected override string TypeId => "itemEnhancementQuest";

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"grade"] = (Integer)Grade,
                [(Text)"count"] = (Integer)_count,
            }.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002

    }
}
