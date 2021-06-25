using System;
using System.Collections.Generic;
using Nekoyume.Model.State;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AvatarInventoryTradableItemRemover : AvatarStateModifier
    {
        private class InnerModel
        {
            public long RequiredBlockIndex { get; }
            public int Count { get; set; }

            public InnerModel(long requiredBlockIndex, int count)
            {
                RequiredBlockIndex = requiredBlockIndex;
                Count = count;
            }

            public InnerModel(InnerModel model)
            {
                RequiredBlockIndex = model.RequiredBlockIndex;
                Count = model.Count;
            }
        }

        private Dictionary<Guid, InnerModel> _items = new Dictionary<Guid, InnerModel>();

        public override bool IsEmpty => _items.Count == 0;

        public AvatarInventoryTradableItemRemover(Guid tradableId, long requiredBlockIndex, int count)
        {
            _items.Add(tradableId, new InnerModel(requiredBlockIndex, count));
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarInventoryTradableItemRemover m))
            {
                return;
            }

            foreach (var item in m._items)
            {
                if (_items.ContainsKey(item.Key))
                {
                    _items[item.Key].Count += item.Value.Count;
                }
                else
                {
                    _items.Add(item.Key, new InnerModel(item.Value));
                }
            }
        }

        public override void Remove(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarInventoryTradableItemRemover m))
            {
                return;
            }

            foreach (var item in m._items)
            {
                if (_items.ContainsKey(item.Key))
                {
                    _items[item.Key].Count -= item.Value.Count;
                    if (_items[item.Key].Count <= 0)
                    {
                        _items.Remove(item.Key);
                    }
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
                state.inventory.RemoveTradableItem(item.Key, item.Value.RequiredBlockIndex, item.Value.Count);
            }

            return state;
        }
    }
}
