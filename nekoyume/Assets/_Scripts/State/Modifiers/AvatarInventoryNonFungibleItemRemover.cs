using System;
using System.Collections.Generic;
using Nekoyume.Model.State;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AvatarInventoryNonFungibleItemRemover : AvatarStateModifier
    {
        private List<Guid> _items = new();

        public override bool IsEmpty => _items.Count == 0;

        public AvatarInventoryNonFungibleItemRemover()
        {
        }

        public AvatarInventoryNonFungibleItemRemover(Guid itemId)
        {
            _items.Add(itemId);
        }

        public void AddItem(Guid itemId)
        {
            _items.Add(itemId);
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (modifier is not AvatarInventoryNonFungibleItemRemover m)
            {
                return;
            }

            foreach (var item in m._items)
            {
                if (_items.Contains(item))
                {
                    _items.Add(item);
                }
            }
        }

        public override void Remove(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (modifier is not AvatarInventoryNonFungibleItemRemover m)
            {
                return;
            }

            foreach (var item in m._items)
            {
                if (_items.Contains(item))
                {
                    _items.Remove(item);
                }
            }
        }

        public override AvatarState Modify(AvatarState state)
        {
            if (state is null)
            {
                return null;
            }

            foreach (var item in _items)
            {
                state.inventory.RemoveNonFungibleItem(item);
            }

            return state;
        }
    }
}
