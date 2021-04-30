using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nekoyume.JsonConvertibles;
using Nekoyume.Model.State;
using UnityEngine;
using UnityEngine.Serialization;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AvatarInventoryNonFungibleItemRemover : AvatarStateModifier
    {
        [SerializeField]
        private List<JsonConvertibleGuid> nonFungibleIds;

        public override bool IsEmpty => nonFungibleIds.Count == 0;

        public AvatarInventoryNonFungibleItemRemover(params Guid[] nonFungibleIdParams)
        {
            nonFungibleIds = new List<JsonConvertibleGuid>();
            foreach (var guid in nonFungibleIdParams)
            {
                nonFungibleIds.Add(new JsonConvertibleGuid(guid));
            }
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarInventoryNonFungibleItemRemover m))
            {
                return;
            }

            foreach (var incoming in m.nonFungibleIds.Where(incoming => !nonFungibleIds.Contains(incoming)))
            {
                nonFungibleIds.Add(incoming);
            }
        }

        public override void Remove(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarInventoryNonFungibleItemRemover m))
            {
                return;
            }

            foreach (var incoming in m.nonFungibleIds.Where(incoming => nonFungibleIds.Contains(incoming)))
            {
                nonFungibleIds.Remove(incoming);
            }
        }

        public override AvatarState Modify(AvatarState state)
        {
            if (state is null)
            {
                return null;
            }

            foreach (var guid in nonFungibleIds)
            {
                state.inventory.RemoveNonFungibleItem(guid.Value);
            }

            return state;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var guid in nonFungibleIds)
            {
                sb.AppendLine(guid.Value.ToString());
            }

            return sb.ToString();
        }
    }
}
