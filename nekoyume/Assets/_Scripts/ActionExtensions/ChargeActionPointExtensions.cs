using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.ActionExtensions
{
    public static class ChargeActionPointExtensions
    {
        public static void PayCost(
            this ChargeActionPoint action,
            IAgent agent,
            States states,
            TableSheets tableSheets,
            IReadOnlyList<Address> updatedAddresses = null,
            bool ignoreNotify = false)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            if (states is null)
            {
                throw new ArgumentNullException(nameof(states));
            }

            if (tableSheets is null)
            {
                throw new ArgumentNullException(nameof(tableSheets));
            }

            var avatarState = states.AvatarStates.Values.FirstOrDefault(e => e.address == action.avatarAddress);
            if (avatarState is null ||
                updatedAddresses is null ||
                !updatedAddresses.Contains(avatarState.address.Derive(LegacyInventoryKey)))
            {
                return;
            }

            var row = tableSheets.MaterialItemSheet.Values.FirstOrDefault(r => r.ItemSubType == ItemSubType.ApStone);
            if (row is null)
            {
                return;
            }

            var nextBlockIndex = agent.BlockIndex + 1;
            avatarState.inventory.RemoveFungibleItem(row.ItemId, nextBlockIndex);

            if (ignoreNotify ||
                avatarState.address != states.CurrentAvatarState.address)
            {
                return;
            }

            ReactiveAvatarState.UpdateInventory(avatarState.inventory);
        }
    }
}
