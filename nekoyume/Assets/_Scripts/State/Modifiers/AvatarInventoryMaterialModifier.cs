using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Libplanet;
using Nekoyume.Model.State;

namespace Nekoyume.State.Modifiers
{
    public class AvatarInventoryMaterialModifier : AvatarStateModifier
    {
        public override bool IsEmpty => _items.Any();
        private readonly Dictionary<HashDigest<SHA256>, int> _items;

        public AvatarInventoryMaterialModifier(Dictionary<HashDigest<SHA256>, int> items)
        {
            _items = items;
        }
        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (modifier is AvatarInventoryMaterialModifier m)
            {
                foreach (var pair in m._items)
                {
                    if (_items.ContainsKey(pair.Key))
                    {
                        _items[pair.Key] += pair.Value;
                    }
                    else
                    {
                        _items[pair.Key] = pair.Value;
                    }
                }
            }
        }

        public override void Remove(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (modifier is AvatarInventoryMaterialModifier m)
            {
                foreach (var pair in m._items.Where(pair => _items.ContainsKey(pair.Key)))
                {
                    _items[pair.Key] -= pair.Value;
                    if (_items[pair.Key] <= 0)
                    {
                        _items.Remove(pair.Key);
                    }
                }
            }
        }

        public override AvatarState Modify(AvatarState state)
        {
            foreach (var pair in _items)
            {
                if (state.inventory.TryGetMaterial(pair.Key, out var inventoryItem))
                {
                    var material = inventoryItem.item;
                    state.inventory.AddItem(material, pair.Value);
                }
            }

            return state;
        }
    }
}
