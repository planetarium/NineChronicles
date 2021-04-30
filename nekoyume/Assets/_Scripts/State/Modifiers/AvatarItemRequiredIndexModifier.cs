using System;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.State.Modifiers
{
    public class AvatarItemRequiredIndexModifier : AvatarStateModifier
    {
        private long _blockIndex;
        private readonly Guid _nonFungibleId;
        public override bool IsEmpty => _blockIndex == 0;

        public AvatarItemRequiredIndexModifier(long blockIndex, Guid nonFungibleId)
        {
            _blockIndex = blockIndex;
            _nonFungibleId = nonFungibleId;
        }

        public AvatarItemRequiredIndexModifier(Guid nonFungibleId)
        {
            _nonFungibleId = nonFungibleId;
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (modifier is AvatarItemRequiredIndexModifier m && m._nonFungibleId == _nonFungibleId)
            {
                _blockIndex += m._blockIndex;
            }
        }

        public override void Remove(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (modifier is AvatarItemRequiredIndexModifier m && m._nonFungibleId == _nonFungibleId)
            {
                _blockIndex -= m._blockIndex;
            }
        }

        public override AvatarState Modify(AvatarState state)
        {
            var item = state.inventory.Items
                .Select(i => i.item)
                .OfType<INonFungibleItem>()
                .FirstOrDefault(i => i.NonFungibleId == _nonFungibleId);
            item?.Update(_blockIndex);
            return state;
        }
    }
}
