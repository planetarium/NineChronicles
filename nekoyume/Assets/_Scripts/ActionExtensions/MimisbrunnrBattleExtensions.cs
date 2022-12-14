using System;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.ActionExtensions
{
    public static class MimisbrunnrBattleExtensions
    {
        public static void PayCost(this MimisbrunnrBattle action, IAgent agent, States states, TableSheets tableSheets)
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
            if (action.AvatarAddress != currentAvatarState.address)
            {
                return;
            }

            var stageSheet = tableSheets.StageSheet;
            if (!stageSheet.TryGetValue(action.StageId, out var row))
            {
                throw new SheetRowNotFoundException(stageSheet.GetType().Name, action.StageId);
            }

            var costAP = row.CostAP * action.PlayCount;
            currentAvatarState.actionPoint -= costAP;

            var inventory = currentAvatarState.inventory;
            for (var i = 0; i < action.Foods.Count; i++)
            {
                var nonFungibleId = action.Foods[i];
                inventory.RemoveNonFungibleItem(nonFungibleId);
            }

            ReactiveAvatarState.UpdateActionPoint(currentAvatarState.actionPoint);
            ReactiveAvatarState.UpdateInventory(currentAvatarState.inventory);
        }
    }
}
