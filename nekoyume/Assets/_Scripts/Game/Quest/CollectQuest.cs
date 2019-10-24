using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.Game.Item;
using Nekoyume.TableData;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class CollectQuest : Quest
    {
        public int current;
        public override QuestType QuestType => QuestType.Obtain;

        private int _itemId;

        public CollectQuest(CollectQuestSheet.Row data) : base(data)
        {
            _itemId = data.ItemId;
        }

        public CollectQuest(Dictionary serialized) : base(serialized)
        {
            _itemId = (int) ((Integer) serialized[(Bencodex.Types.Text) "itemId"]).Value;
            current = (int) ((Integer) serialized[(Bencodex.Types.Text) "current"]).Value;
        }

        public override void Check()
        {
            if (Complete)
                return;
            Complete = current >= Goal;
        }

        public override string ToInfo()
        {
            var format = LocalizationManager.Localize("QUEST_COLLECT_CURRENT_INFO_FORMAT");
            var itemName = LocalizationManager.LocalizeItemName(_itemId);
            return string.Format(format, itemName, current, Goal);
        }

        protected override string TypeId => "collectQuest";

        public void Update(List<ItemBase> rewards)
        {
            current += rewards.Count(i => i.Data.Id == _itemId);
        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "current"] = (Integer) current,
                [(Text) "itemId"] = (Integer) _itemId,
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));

    }
}
