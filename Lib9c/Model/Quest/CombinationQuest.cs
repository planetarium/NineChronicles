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
    public class CombinationQuest : Quest
    {
        public ItemType ItemType
        {
            get
            {
                if (_serializedItemType is { })
                {
                    _itemType = (ItemType) (int) _serializedItemType;
                    _serializedItemType = null;
                }

                return _itemType;
            }
        }

        public ItemSubType ItemSubType
        {
            get
            {
                if (_serializedItemSubType is { })
                {
                    _itemSubType = (ItemSubType) (int) _serializedItemSubType;
                    _serializedItemSubType = null;
                }

                return _itemSubType;
            }
        }
        private Integer? _serializedItemType;
        private ItemType _itemType;
        private Integer? _serializedItemSubType;
        private ItemSubType _itemSubType;
        public override QuestType QuestType => QuestType.Craft;

        public CombinationQuest(CombinationQuestSheet.Row data, QuestReward reward)
            : base(data, reward)
        {
            _itemType = data.ItemType;
            _itemSubType = data.ItemSubType;
        }

        public CombinationQuest(Dictionary serialized) : base(serialized)
        {
            _serializedItemType = (Integer)serialized["itemType"];
            _serializedItemSubType = (Integer)serialized["itemSubType"];
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

        protected override string TypeId => "combinationQuest";

        public void Update(List<ItemBase> items)
        {
            if (Complete)
                return;

            _current += items.Count(i => i.ItemType == ItemType && i.ItemSubType == ItemSubType);
            Check();
        }

        public override IValue Serialize() =>
            ((Dictionary) base.Serialize())
            .Add("itemType", _serializedItemType ?? (int) ItemType)
            .Add("itemSubType", _serializedItemSubType ?? (int) ItemSubType);
    }
}
