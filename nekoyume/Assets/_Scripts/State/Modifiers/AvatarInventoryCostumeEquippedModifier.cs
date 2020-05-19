using System;
using System.Linq;
using Nekoyume.JsonConvertibles;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AvatarInventoryCostumeEquippedModifier : AvatarStateModifier
    {
        [Serializable]
        public class InnerDictionary : JsonConvertibleDictionary<int, bool>
        {
        }

        [SerializeField]
        private InnerDictionary dictionary;

        public override bool IsEmpty => dictionary.Count == 0;

        public AvatarInventoryCostumeEquippedModifier(int id, bool equipped)
        {
            dictionary = new InnerDictionary();
            dictionary.Value.Add(id, equipped);
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarInventoryCostumeEquippedModifier m))
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
            if (!(modifier is AvatarInventoryCostumeEquippedModifier m))
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

            var equipments = state.inventory.Items
                .Select(inventoryItem => inventoryItem.item)
                .OfType<Costume>()
                .ToArray();

            foreach (var pair in dictionary.Value)
            {
                foreach (var equipment in
                    equipments.Where(equipment => equipment.Data.Id == pair.Key))
                {
                    equipment.equipped = pair.Value;
                }
            }

            return state;
        }
    }
}
