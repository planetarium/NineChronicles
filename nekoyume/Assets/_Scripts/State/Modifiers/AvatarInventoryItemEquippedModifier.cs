using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.JsonConvertibles;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AvatarInventoryItemEquippedModifier : AvatarStateModifier
    {
        [Serializable]
        public class InnerDictionary : JsonConvertibleDictionary<JsonConvertibleGuid, bool>
        {
        }

        [SerializeField]
        private InnerDictionary dictionary;

        public override bool IsEmpty => dictionary.Count == 0;

        public AvatarInventoryItemEquippedModifier(Guid nonFungibleId, bool equipped)
        {
            dictionary = new InnerDictionary();
            dictionary.Value.Add(new JsonConvertibleGuid(nonFungibleId), equipped);
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarInventoryItemEquippedModifier m))
            {
                return;
            }

            foreach (var pair in m.dictionary.Value)
            {
                dictionary.Value[pair.Key] = pair.Value;
            }
        }

        public override void Remove(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarInventoryItemEquippedModifier m))
            {
                return;
            }

            foreach (var pair in m.dictionary.Value)
            {
                var key = pair.Key;
                if (dictionary.Value.ContainsKey(key))
                {
                    dictionary.Value.Remove(key);
                }
            }
        }

        public override AvatarState Modify(AvatarState state)
        {
            if (state is null)
            {
                return null;
            }

            var shouldRemoveKeys = new List<JsonConvertibleGuid>();
            var nonFungibleItems = state.inventory.Items
                .Select(inventoryItem => inventoryItem.item)
                .OfType<INonFungibleItem>()
                .ToArray();

            foreach (var pair in dictionary.Value)
            {
                var nonFungibleItem = nonFungibleItems
                    .FirstOrDefault(item => item.NonFungibleId.Equals(pair.Key.Value));
                if (nonFungibleItem is null ||
                    !(nonFungibleItem is IEquippableItem equippableItem))
                {
                    shouldRemoveKeys.Add(pair.Key);
                }
                else
                {
                    if (pair.Value)
                    {
                        equippableItem.Equip();
                    }
                    else
                    {
                        equippableItem.Unequip();
                    }
                }
            }

            if (shouldRemoveKeys.Count > 0)
            {
                foreach (var shouldRemoveKey in shouldRemoveKeys)
                {
                    dictionary.Value.Remove(shouldRemoveKey);
                }

                dirty = true;
            }

            return state;
        }
    }
}
