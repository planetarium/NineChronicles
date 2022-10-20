using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.ActionExtensions
{
    public static class CombinationEquipmentExtensions
    {
        public static void PayCost(
            this CombinationEquipment action,
            IAgent agent,
            States states,
            TableSheets tableSheets)
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
            // var equipmentItemRecipeSheet = tableSheets.EquipmentItemRecipeSheet;
            // if (!equipmentItemRecipeSheet.TryGetValue(action.recipeId, out var recipeRow))
            // {
            //     throw new SheetRowNotFoundException(nameof(EquipmentItemRecipeSheet), action.recipeId);
            // }
            //
            // var costNCG = recipeRow.RequiredGold;
            // var costAP = recipeRow.RequiredActionPoint;
            // var costFungibleItems = new Dictionary<int, int>
            // {
            //     { recipeRow.MaterialId, recipeRow.MaterialCount },
            // };
            //
            // if (action.subRecipeId.HasValue)
            // {
            //     var equipmentItemSubRecipeSheetV2 = tableSheets.EquipmentItemSubRecipeSheetV2;
            //     if (!equipmentItemSubRecipeSheetV2.TryGetValue(action.subRecipeId.Value, out var subRecipeRow))
            //     {
            //         throw new SheetRowNotFoundException(
            //             nameof(EquipmentItemSubRecipeSheetV2),
            //             action.subRecipeId.Value);
            //     }
            //
            //     costNCG += subRecipeRow.RequiredGold;
            //     costAP += subRecipeRow.RequiredActionPoint;
            //     for (var i = subRecipeRow.Materials.Count; i > 0; i--)
            //     {
            //         var materialInfo = subRecipeRow.Materials[i - 1];
            //         costFungibleItems.Add(materialInfo.Id, materialInfo.Count);
            //     }
            // }
            //
            // currentAvatarState.actionPoint -= costAP;
            //
            // foreach (var pair in costFungibleItems)
            // {
            //     if (tableSheets.MaterialItemSheet.TryGetValue(pair.Key, out var materialRow))
            //     {
            //         currentAvatarState.inventory.RemoveFungibleItem(materialRow.ItemId, pair.Value);
            //     }
            // }
            //
            // var equipmentItemSheet = tableSheets.EquipmentItemSheet;
            // if (!equipmentItemSheet.TryGetValue(recipeRow.ResultEquipmentId, out var equipmentRow))
            // {
            //     throw new SheetRowNotFoundException(nameof(equipmentItemSheet), recipeRow.ResultEquipmentId);
            // }
            //
            // var nextBlockIndex = agent.BlockIndex + 1;
            // var endBlockIndex = nextBlockIndex + recipeRow.RequiredBlockIndex;
            // var equipment = (Equipment)ItemFactory.CreateItemUsable(
            //     equipmentRow,
            //     default,
            //     endBlockIndex
            // );
            //
            // var attachmentResult = new CombinationConsumable5.ResultModel
            // {
            //     id = default,
            //     actionPoint = costAP,
            //     materials = costFungibleItems.ToDictionary(
            //         e => ItemFactory.CreateMaterial(tableSheets.MaterialItemSheet, e.Key),
            //         e => e.Value),
            //     itemUsable = equipment,
            //     recipeId = action.recipeId,
            // };
            //
            // var slotState = states.GetCombinationSlotState(action.avatarAddress,  nextBlockIndex)?[action.slotIndex];
            // if (slotState is null)
            // {
            //     throw new Exception(
            //         $"slot state is null: block index({nextBlockIndex}), slot index({action.slotIndex})");
            // }
            //
            // // FIXME: Add non-serialize field for slot state to use for UI
            // slotState.Update(attachmentResult, nextBlockIndex, endBlockIndex);
            // states.UpdateCombinationSlotState(action.avatarAddress, action.slotIndex, slotState);
            //
            // var goldBalanceState = new GoldBalanceState(states.GoldBalanceState.address,
            //     states.GoldBalanceState.Gold.Currency * costNCG);
            // states.SetGoldBalanceState(goldBalanceState);
            // ReactiveAvatarState.UpdateActionPoint(currentAvatarState.actionPoint);
            // ReactiveAvatarState.UpdateInventory(currentAvatarState.inventory);
        }
    }
}
