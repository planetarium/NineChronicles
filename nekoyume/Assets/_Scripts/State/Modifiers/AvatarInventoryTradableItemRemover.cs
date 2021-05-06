using System;
using System.Collections.Generic;
using Nekoyume.JsonConvertibles;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AvatarInventoryTradableItemRemover : AvatarStateModifier
    {
        [Serializable]
        public class InnerDictionary : JsonConvertibleDictionary<JsonConvertibleGuid, int>
        {
        }

        [SerializeField]
        private InnerDictionary innerDictionary;

        public override bool IsEmpty => innerDictionary.Value.Count == 0;

        public AvatarInventoryTradableItemRemover(Guid tradableId, int count = 1)
        {
            if (count is 0)
            {
                innerDictionary = new InnerDictionary();
                return;
            }

            innerDictionary = new InnerDictionary();
            innerDictionary.Value.Add(new JsonConvertibleGuid(tradableId), count);
        }

        public AvatarInventoryTradableItemRemover(Dictionary<Guid, int> dictionary)
        {
            innerDictionary = new InnerDictionary();
            foreach (var pair in dictionary)
            {
                if (pair.Value is 0)
                {
                    continue;
                }

                innerDictionary.Value.Add(new JsonConvertibleGuid(pair.Key), pair.Value);
            }
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarInventoryTradableItemRemover m))
            {
                return;
            }

            foreach (var pair in m.innerDictionary.Value)
            {
                var key = pair.Key;
                if (innerDictionary.Value.ContainsKey(key))
                {
                    innerDictionary.Value[key] += pair.Value;
                }
                else
                {
                    innerDictionary.Value.Add(key, pair.Value);
                }
            }
        }

        public override void Remove(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarInventoryTradableItemRemover m))
            {
                return;
            }

            foreach (var pair in m.innerDictionary.Value)
            {
                var key = pair.Key;
                if (!innerDictionary.Value.ContainsKey(key))
                {
                    continue;
                }

                innerDictionary.Value[key] -= pair.Value;
                if (innerDictionary.Value[key] <= 0)
                {
                    innerDictionary.Value.Remove(key);
                }
            }
        }

        public override AvatarState Modify(AvatarState state)
        {
            if (state is null)
            {
                return null;
            }

            foreach (var pair in innerDictionary.Value)
            {
                state.inventory.RemoveTradableItem(pair.Key.Value, pair.Value);
            }

            return state;
        }
    }
}
