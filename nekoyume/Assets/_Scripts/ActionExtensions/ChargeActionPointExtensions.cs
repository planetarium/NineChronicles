using System;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.State;

namespace Nekoyume.ActionExtensions
{
    public static class ChargeActionPointExtensions
    {
        public static void PayCost(this ChargeActionPoint action, IAgent agent, States states, TableSheets tableSheets)
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

            var currentAvatarState = states.CurrentAvatarState;
            if (action.avatarAddress != currentAvatarState.address)
            {
                return;
            }

            var row = tableSheets.MaterialItemSheet.Values.FirstOrDefault(r => r.ItemSubType == ItemSubType.ApStone);
            if (row is null)
            {
                return;
            }

            var nextBlockIndex = agent.BlockIndex + 1;
            currentAvatarState.inventory.RemoveFungibleItem(row.ItemId, nextBlockIndex);

            ReactiveAvatarState.UpdateInventory(currentAvatarState.inventory);
        }
    }
}
