using System;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Model.State;
using Nekoyume.State;

namespace Nekoyume.ActionExtensions
{
    public static class BuyExtensions
    {
        public static void PayCost(this Buy action, IAgent agent, States states, TableSheets tableSheets)
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

            var avatarState = states.AvatarStates.Values.FirstOrDefault(e => e.address == action.buyerAvatarAddress);
            if (avatarState is null)
            {
                return;
            }

            var gold = action.purchaseInfos.Aggregate(
                states.GoldBalanceState.Gold,
                (current, purchaseInfo) => current - purchaseInfo.Price);

            var state = new GoldBalanceState(states.GoldBalanceState.address, gold);
            states.SetGoldBalanceState(state);
        }
    }
}
