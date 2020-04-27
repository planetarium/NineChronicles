using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Libplanet;
using Nekoyume.JsonConvertibles;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AvatarInventoryFungibleItemRemover : AvatarStateModifier
    {
        [Serializable]
        public class InnerHashDigest : JsonConvertibleHashDigest<SHA256>
        {
            public InnerHashDigest(HashDigest<SHA256> hashDigest) : base(hashDigest)
            {
            }
        }

        [Serializable]
        public class InnerDictionary : JsonConvertibleDictionary<InnerHashDigest, int>
        {
        }

        [SerializeField]
        private InnerDictionary idAndCountDictionary;

        public override bool IsEmpty => idAndCountDictionary.Value.Count == 0;

        public AvatarInventoryFungibleItemRemover(HashDigest<SHA256> id, int count)
        {
            if (count is 0)
            {
                idAndCountDictionary = new InnerDictionary();

                return;
            }

            idAndCountDictionary = new InnerDictionary();
            idAndCountDictionary.Value.Add(new InnerHashDigest(id), count);
        }

        public AvatarInventoryFungibleItemRemover(Dictionary<HashDigest<SHA256>, int> idAndCountDictionary)
        {
            this.idAndCountDictionary = new InnerDictionary();
            foreach (var pair in idAndCountDictionary)
            {
                if (pair.Value is 0)
                    continue;

                this.idAndCountDictionary.Value.Add(new InnerHashDigest(pair.Key), pair.Value);
            }
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarInventoryFungibleItemRemover m))
                return;

            foreach (var pair in m.idAndCountDictionary.Value)
            {
                var key = pair.Key;
                if (idAndCountDictionary.Value.ContainsKey(key))
                {
                    idAndCountDictionary.Value[key] += pair.Value;
                }
                else
                {
                    idAndCountDictionary.Value.Add(key, pair.Value);
                }
            }
        }

        public override void Remove(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarInventoryFungibleItemRemover m))
                return;

            foreach (var pair in m.idAndCountDictionary.Value)
            {
                var key = pair.Key;
                if (!idAndCountDictionary.Value.ContainsKey(key))
                    continue;

                idAndCountDictionary.Value[key] -= pair.Value;
                if (idAndCountDictionary.Value[key] <= 0)
                    idAndCountDictionary.Value.Remove(key);
            }
        }

        public override AvatarState Modify(AvatarState state)
        {
            if (state is null)
                return null;

            foreach (var pair in idAndCountDictionary.Value)
            {
                state.inventory.RemoveMaterial(pair.Key.Value, pair.Value);
            }

            return state;
        }
    }
}
