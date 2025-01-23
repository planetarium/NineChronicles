using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Libplanet.Common;
using Nekoyume.JsonConvertibles;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AvatarInventoryFungibleItemRemover : AvatarStateModifier
    {
        [Serializable]
        public class JsonConvertibleFungibleId : JsonConvertibleHashDigest<SHA256>
        {
            public JsonConvertibleFungibleId(HashDigest<SHA256> fungibleId) : base(fungibleId)
            {
            }
        }

        [Serializable]
        public class InnerDictionary : JsonConvertibleDictionary<JsonConvertibleFungibleId, int>
        {
        }

        [SerializeField]
        private InnerDictionary innerDictionary;

        public override bool IsEmpty => innerDictionary.Value.Count == 0;

        public AvatarInventoryFungibleItemRemover()
        {
            innerDictionary = new InnerDictionary();
        }

        public AvatarInventoryFungibleItemRemover(HashDigest<SHA256> fungibleId, int count)
        {
            if (count is 0)
            {
                innerDictionary = new InnerDictionary();
                return;
            }

            innerDictionary = new InnerDictionary();
            innerDictionary.Value.Add(new JsonConvertibleFungibleId(fungibleId), count);
        }

        public AvatarInventoryFungibleItemRemover(Dictionary<HashDigest<SHA256>, int> dictionary)
        {
            innerDictionary = new InnerDictionary();
            foreach (var pair in dictionary)
            {
                if (pair.Value is 0)
                {
                    continue;
                }

                innerDictionary.Value.Add(new JsonConvertibleFungibleId(pair.Key), pair.Value);
            }
        }

        public void AddItem(HashDigest<SHA256> fungibleId, int count)
        {
            if (count is 0)
            {
                return;
            }

            if (innerDictionary.Value.ContainsKey(new JsonConvertibleFungibleId(fungibleId)))
            {
                innerDictionary.Value[new JsonConvertibleFungibleId(fungibleId)] += count;
            }
            else
            {
                innerDictionary.Value.Add(new JsonConvertibleFungibleId(fungibleId), count);
            }
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarInventoryFungibleItemRemover m))
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
            if (!(modifier is AvatarInventoryFungibleItemRemover m))
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
                state.inventory.RemoveFungibleItem(
                    pair.Key.Value,
                    Game.Game.instance.Agent.BlockIndex,
                    pair.Value);
            }

            return state;
        }
    }
}
