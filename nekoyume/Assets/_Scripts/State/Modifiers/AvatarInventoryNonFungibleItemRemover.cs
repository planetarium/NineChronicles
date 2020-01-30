using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nekoyume.JsonConvertibles;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AvatarInventoryNonFungibleItemRemover : AvatarStateModifier
    {
        [SerializeField]
        private List<JsonConvertibleGuid> guidList;

        public override bool IsEmpty => guidList.Count == 0;

        public AvatarInventoryNonFungibleItemRemover(params Guid[] guidParams)
        {
            guidList = new List<JsonConvertibleGuid>();
            foreach (var guid in guidParams)
            {
                guidList.Add(new JsonConvertibleGuid(guid));
            }
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarInventoryNonFungibleItemRemover m))
                return;

            foreach (var incoming in m.guidList.Where(incoming => !guidList.Contains(incoming)))
            {
                guidList.Add(incoming);
            }
        }

        public override void Remove(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarInventoryNonFungibleItemRemover m))
                return;

            foreach (var incoming in m.guidList.Where(incoming => guidList.Contains(incoming)))
            {
                guidList.Remove(incoming);
            }
        }

        public override AvatarState Modify(AvatarState state)
        {
            if (state is null)
                return null;

            foreach (var guid in guidList)
            {
                state.inventory.RemoveNonFungibleItem(guid.Value);
            }

            return state;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var guid in guidList)
            {
                sb.AppendLine(guid.Value.ToString());
            }

            return sb.ToString();
        }
    }
}
