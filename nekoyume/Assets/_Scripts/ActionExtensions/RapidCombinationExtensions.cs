using System;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.State;

namespace Nekoyume.ActionExtensions
{
    public static class RapidCombinationExtensions
    {
        public static void PayCost(this RapidCombination action, IAgent agent, States states, TableSheets tableSheets)
        {
            // NOTE: ignore now
            return;
            // if (action is null)
            // {
            //     throw new ArgumentNullException(nameof(action));
            // }
            //
            // if (agent is null)
            // {
            //     throw new ArgumentNullException(nameof(agent));
            // }
            //
            // if (states is null)
            // {
            //     throw new ArgumentNullException(nameof(states));
            // }
            //
            // if (tableSheets is null)
            // {
            //     throw new ArgumentNullException(nameof(tableSheets));
            // }
            //
            // var currentAvatarState = states.CurrentAvatarState;
            // if (action.avatarAddress != currentAvatarState.address)
            // {
            //     return;
            // }
            //
            // var nextBlockIndex = agent.BlockIndex + 1;
            // var slotState = states.GetCombinationSlotState(nextBlockIndex)?[action.slotIndex];
            // if (slotState is null)
            // {
            //     throw new Exception(
            //         $"slot state is null: block index({nextBlockIndex}), slot index({action.slotIndex})");
            // }
            //
            // var diff = slotState.Result.itemUsable.RequiredBlockIndex - nextBlockIndex;
            // var count = RapidCombination0.CalculateHourglassCount(states.GameConfigState, diff);
            // var materialItemSheet = tableSheets.MaterialItemSheet;
            // var hourGlassRow = materialItemSheet.Values.FirstOrDefault(r => r.ItemSubType == ItemSubType.Hourglass);
            // if (hourGlassRow is null)
            // {
            //     return;
            // }
            //
            // currentAvatarState.inventory.RemoveFungibleItem(hourGlassRow.ItemId, nextBlockIndex, count);
            //
            // // FIXME: Add non-serialize field for slot state to use for UI
            // slotState.Update(slotState.Result, slotState.StartBlockIndex, nextBlockIndex);
            // states.UpdateCombinationSlotState(action.avatarAddress, action.slotIndex, slotState);
            //
            // ReactiveAvatarState.UpdateInventory(currentAvatarState.inventory);
        }
    }
}
