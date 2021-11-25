using System;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.ActionExtensions
{
    public static class HackAndSlashExtensions
    {
        public static void PayCost(this HackAndSlash action, IAgent agent, States states, TableSheets tableSheets)
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

            var stageSheet = tableSheets.StageSheet;
            if (!stageSheet.TryGetValue(action.stageId, out var row))
            {
                throw new SheetRowNotFoundException(stageSheet.GetType().Name, action.stageId);
            }

            var costAP = row.CostAP * action.playCount;
            currentAvatarState.actionPoint -= costAP;

            var inventory = currentAvatarState.inventory;
            for (var i = 0; i < action.foods.Count; i++)
            {
                var nonFungibleId = action.foods[i];
                inventory.RemoveNonFungibleItem(nonFungibleId);
            }

            if (currentAvatarState != states.CurrentAvatarState)
            {
                return;
            }

            ReactiveAvatarState.UpdateActionPoint(currentAvatarState.actionPoint);
            ReactiveAvatarState.UpdateInventory(currentAvatarState.inventory);
        }
    }
}
