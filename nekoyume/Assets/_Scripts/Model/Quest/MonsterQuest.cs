using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Quest
{
    [Serializable]
    public class MonsterQuest : Quest
    {
        private readonly int _monsterId;

        public MonsterQuest(MonsterQuestSheet.Row data, QuestReward reward) 
            : base(data, reward)
        {
            _monsterId = data.MonsterId;
        }

        public MonsterQuest(Dictionary serialized) : base(serialized)
        {
            _monsterId = (int)((Integer)serialized["monsterId"]).Value;
        }

        public override QuestType QuestType => QuestType.Adventure;

        public override void Check()
        {
            if (Complete)
                return;

            Complete = _current >= Goal;
        }

        public override string GetName()
        {
            var format = LocalizationManager.Localize("QUEST_MONSTER_FORMAT");
            return string.Format(format, LocalizationManager.LocalizeCharacterName(_monsterId));
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

            monsterMap.TryGetValue(_monsterId, out _current);
            Check();
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"monsterId"] = (Integer)_monsterId,
            }.Union((Dictionary)base.Serialize()));
    }
}
