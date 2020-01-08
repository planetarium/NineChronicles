using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.Model;
using Nekoyume.TableData;
using Org.BouncyCastle.Asn1.CryptoPro;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class MonsterQuest: Quest
    {
        private readonly int _monsterId;

        public MonsterQuest(MonsterQuestSheet.Row data) : base(data)
        {
            _monsterId = data.MonsterId;
        }

        public MonsterQuest(Bencodex.Types.Dictionary serialized) : base(serialized)
        {
            _monsterId = (int) ((Integer) serialized["monsterId"]).Value;
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
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "monsterId"] = (Integer) _monsterId,
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));
    }
}
