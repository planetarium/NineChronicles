using System;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.State.Modifiers
{
    public class AvatarItemRequiredIndexModifier : AvatarStateModifier
    {
        private long _blockIndex;
        private readonly Guid _tradableId;
        public override bool IsEmpty => _blockIndex == 0;

        public AvatarItemRequiredIndexModifier(long blockIndex, Guid tradableId)
        {
            _blockIndex = blockIndex;
            _tradableId = tradableId;
        }

        public AvatarItemRequiredIndexModifier(Guid tradableId)
        {
            _tradableId = tradableId;
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (modifier is AvatarItemRequiredIndexModifier m && m._tradableId == _tradableId)
            {
                _blockIndex += m._blockIndex;
            }
        }

        public override void Remove(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (modifier is AvatarItemRequiredIndexModifier m && m._tradableId == _tradableId)
            {
                _blockIndex -= m._blockIndex;
            }
        }

        public override AvatarState Modify(AvatarState state)
        {
            var item = state.inventory.Items
                .Select(i => i.item)
                .OfType<ITradableItem>()
                .FirstOrDefault(i => i.TradableId == _tradableId);
            if (!(item is null))
            {
                item.RequiredBlockIndex = _blockIndex;
            }

            return state;
        }
    }
}
