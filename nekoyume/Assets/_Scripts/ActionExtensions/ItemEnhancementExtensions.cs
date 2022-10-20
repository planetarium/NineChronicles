using System;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.ActionExtensions
{
    public static class ItemEnhancementExtensions
    {
        public static void PayCost(this ItemEnhancement action, IAgent agent, States states, TableSheets tableSheets)
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
            // currentAvatarState.inventory.TryGetNonFungibleItem(action.itemId, out ItemUsable enhancementItem);
            // currentAvatarState.inventory.RemoveNonFungibleItem(action.itemId);
            // currentAvatarState.inventory.RemoveNonFungibleItem(action.materialId);
            // var enhancementEquipment = (Equipment)enhancementItem;
            // var enhancementCostSheet = tableSheets.EnhancementCostSheetV2;
            // if (!ItemEnhancement.TryGetRow(enhancementEquipment, enhancementCostSheet, out var costRow))
            // {
            //     throw new SheetRowNotFoundException(
            //         nameof(EnhancementCostSheetV2),
            //         "equipment",
            //         $"grade({enhancementEquipment.Grade}), level({enhancementEquipment.level + 1}), item sub type({enhancementEquipment.ItemSubType.ToString()})");
            // }
            //
            // var costNCG = costRow.Cost;
            // var costActionPoint = ItemEnhancement.GetRequiredAp();
            // var enhancementResult = new ItemEnhancement.ResultModel
            // {
            //     preItemUsable = enhancementItem,
            //     itemUsable = enhancementItem,
            //     materialItemIdList = new[] { action.materialId },
            //     actionPoint = costActionPoint,
            //     enhancementResult = ItemEnhancement.EnhancementResult.Success,
            //     gold = costNCG,
            // };
            //
            // var nextBlockIndex = agent.BlockIndex + 1;
            // var slotState = states.GetCombinationSlotState(nextBlockIndex)?[action.slotIndex];
            // if (slotState is null)
            // {
            //     throw new Exception(
            //         $"slot state is null: block index({nextBlockIndex}), slot index({action.slotIndex})");
            // }
            //
            // var endBlockIndex = nextBlockIndex + ItemEnhancement.GetRequiredBlockCount(
            //     costRow,
            //     ItemEnhancement.EnhancementResult.Success);
            // // FIXME: Add non-serialize field for slot state to use for UI
            // slotState.Update(enhancementResult, nextBlockIndex, endBlockIndex);
            // states.UpdateCombinationSlotState(action.avatarAddress, action.slotIndex, slotState);
            //
            // var goldBalanceState = new GoldBalanceState(states.GoldBalanceState.address,
            //     states.GoldBalanceState.Gold.Currency * costNCG);
            // states.SetGoldBalanceState(goldBalanceState);
            // ReactiveAvatarState.UpdateInventory(currentAvatarState.inventory);
        }
    }
}
