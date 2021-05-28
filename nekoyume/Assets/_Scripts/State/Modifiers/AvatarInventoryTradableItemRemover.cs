using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.State;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AvatarInventoryTradableItemRemover : AvatarStateModifier
    {
        private class InnerModel
        {
            public Guid Guid { get; }
            public long RequiredBlockIndex { get; }
            public int Count { get; set; }

            public InnerModel(Guid guid, long requiredBlockIndex, int count)
            {
                Guid = guid;
                RequiredBlockIndex = requiredBlockIndex;
                Count = count;
            }
        }

        private List<InnerModel> _items = new List<InnerModel>();

        public override bool IsEmpty => _items.Count == 0;

        public AvatarInventoryTradableItemRemover(Guid tradableId, long requiredBlockIndex, int count)
        {
            _items.Add(new InnerModel(tradableId, requiredBlockIndex, count));
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarInventoryTradableItemRemover m))
            {
                return;
            }

            foreach (var item in m._items)
            {
                var model = _items.FirstOrDefault(x => x.Guid == item.Guid);
                if (model == null)
                {
                    _items.Add(item);
                }
                else
                {
                    model.Count += item.Count;
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
                var model = _items.FirstOrDefault(x => x.Guid == item.Guid);
                if (model != null)
                {
                    model.Count -= item.Count;
                    if (model.Count <= 0)
                    {
                        _items.Remove(model);
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
                state.inventory.RemoveTradableItem(item.Guid, item.RequiredBlockIndex, item.Count);
            }

            return state;
        }
    }
}
