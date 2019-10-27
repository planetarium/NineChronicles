using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.EnumType;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class GeneralQuest : Quest
    {
        public readonly QuestEventType Event;
        private int _current;

        public GeneralQuest(GeneralQuestSheet.Row data) : base(data)
        {
            Event = data.Event;
        }

        public GeneralQuest(Dictionary serialized) : base(serialized)
        {
            _current = (int) ((Integer) serialized[(Bencodex.Types.Text) "current"]).Value;
            Event = (QuestEventType) (int) ((Integer) serialized[(Bencodex.Types.Text) "event"]).Value;
        }

        public override void Check()
        {
            Complete = _current >= Goal;
        }

        public override string ToInfo()
        {
            var format = LocalizationManager.Localize($"QUEST_GENERAL_{Event}_FORMAT");
            return string.Format(format, _current, Goal);
        }

        protected override string TypeId => "generalQuest";

        public void Update(CollectionMap eventMap)
        {
            var key = (int) Event;
            eventMap.TryGetValue(key, out var current);
            _current = current;
            Check();
        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "current"] = (Integer) _current,
                [(Text) "event"] = (Integer) (int) Event,
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));

    }
}
