using System;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.State;

namespace Nekoyume.ActionExtensions
{
    public static class SellExtensions
    {
        public static void PayCost(this Sell action, IAgent agent, States states, TableSheets tableSheets)
        {
            // NOTE: ignore now
            return;
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

            var avatarState = states.AvatarStates.Values.FirstOrDefault(e => e.address == action.sellerAvatarAddress);
            if (avatarState is null)
            {
                return;
            }

            var nextBlockIndex = agent.BlockIndex + 1;
            avatarState.inventory.RemoveTradableItem(action.tradableId, nextBlockIndex, action.count);

            var currentAvatarState = states.CurrentAvatarState;
            if (avatarState.address != currentAvatarState.address)
            {
                return;
            }

            ReactiveAvatarState.UpdateInventory(currentAvatarState.inventory);
        }
    }
}
