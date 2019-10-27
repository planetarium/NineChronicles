using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.Game.Item;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.Game.Quest
{
    public class ItemGradeQuest : Quest
    {
        public readonly int Grade;
        private int _count;
        private readonly int _goal;
        private List<int> _itemIds = new List<int>();

        public ItemGradeQuest(ItemGradeQuestSheet.Row data) : base(data)
        {
            _goal = data.Goal;
            Grade = data.Grade;
        }

        public ItemGradeQuest(Dictionary serialized) : base(serialized)
        {
            Grade = (int) ((Integer) serialized[(Bencodex.Types.Text) "grade"]).Value;
            _count = (int) ((Integer) serialized[(Bencodex.Types.Text) "count"]).Value;
            _goal = (int) ((Integer) serialized[(Bencodex.Types.Text) "goal"]).Value;
            _itemIds = serialized[(Bencodex.Types.Text) "itemIds"].ToList(i => (int) ((Integer) i).Value);
        }

        public override void Check()
        {
            Complete = _count >= Goal;
        }

        public override string ToInfo()
        {
            var format = LocalizationManager.Localize("QUEST_ITEM_GRADE_FORMAT");
            return string.Format(format, Grade, _count, Goal);
        }

        public void Update(ItemUsable itemUsable)
        {
            if (!_itemIds.Contains(itemUsable.Data.Id))
            {
                _count++;
                _itemIds.Add(itemUsable.Data.Id);
            }
            Check();
        }

        protected override string TypeId => "itemGradeQuest";

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "grade"] = (Integer) Grade,
                [(Text) "count"] = (Integer) _count,
                [(Text) "goal"] = (Integer) _goal,
                [(Text) "itemIds"] = (Bencodex.Types.List) _itemIds.Select(i => (Integer) i).Serialize(),
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));

    }
}
