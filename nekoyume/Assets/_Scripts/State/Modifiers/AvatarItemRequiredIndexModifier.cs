using System;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.State.Modifiers
{
    public class AvatarItemRequiredIndexModifier : AvatarStateModifier
    {
        private long _blockIndex;
        private readonly Guid _itemId;
        public override bool IsEmpty => _blockIndex == 0;

        public AvatarItemRequiredIndexModifier(long blockIndex, Guid itemId)
        {
            _blockIndex = blockIndex;
            _itemId = itemId;
        }

        public AvatarItemRequiredIndexModifier(Guid itemId)
        {
            _itemId = itemId;
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarItemRequiredIndexModifier m) || m._itemId != _itemId)
                return;

            _blockIndex += m._blockIndex;
        }

        public override void Remove(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarItemRequiredIndexModifier m) || m._itemId != _itemId)
                return;

            _blockIndex -= m._blockIndex;
        }

        public override AvatarState Modify(AvatarState state)
        {
            var item = state.inventory.Items
                .Select(i => i.item)
                .OfType<ItemUsable>()
                .FirstOrDefault(i => i.ItemId == _itemId);
            item?.Update(_blockIndex);
            return state;
        }
    }
}
