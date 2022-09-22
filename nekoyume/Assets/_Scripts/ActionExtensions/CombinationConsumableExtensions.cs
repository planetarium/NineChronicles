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
    public static class CombinationConsumableExtensions
    {
        public static void PayCost(
            this CombinationConsumable action,
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
            // var consumableItemRecipeSheet = tableSheets.ConsumableItemRecipeSheet;
            // if (!consumableItemRecipeSheet.TryGetValue(action.recipeId, out var recipeRow))
            // {
            //     throw new SheetRowNotFoundException(nameof(ConsumableItemRecipeSheet), action.recipeId);
            // }
            //
            // var costNCG = (long)recipeRow.RequiredGold;
            // var costAP = recipeRow.RequiredActionPoint;
            // var costFungibleItems = new Dictionary<int, int>();
            // for (var i = recipeRow.Materials.Count; i > 0; i--)
            // {
            //     var materialInfo = recipeRow.Materials[i - 1];
            //     costFungibleItems.Add(materialInfo.Id, materialInfo.Count);
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
            // var consumableItemSheet = tableSheets.ConsumableItemSheet;
            // if (!consumableItemSheet.TryGetValue(recipeRow.ResultConsumableItemId, out var consumableRow))
            // {
            //     throw new SheetRowNotFoundException(nameof(consumableItemSheet), recipeRow.ResultConsumableItemId);
            // }
            //
            // var nextBlockIndex = agent.BlockIndex + 1;
            // var endBlockIndex = nextBlockIndex + recipeRow.RequiredBlockIndex;
            // var consumable = (Consumable)ItemFactory.CreateItemUsable(
            //     consumableRow,
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
            //     itemUsable = consumable,
            //     recipeId = action.recipeId,
            // };
            //
            // var slotState = states.GetCombinationSlotState(nextBlockIndex)?[action.slotIndex];
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
