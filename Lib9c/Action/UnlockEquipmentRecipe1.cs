using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("unlock_equipment_recipe")]
    public class UnlockEquipmentRecipe1: GameAction, IUnlockEquipmentRecipeV1
    {
        public List<int> RecipeIds = new List<int>();
        public Address AvatarAddress;

        IEnumerable<int> IUnlockEquipmentRecipeV1.RecipeIds => RecipeIds;
        Address IUnlockEquipmentRecipeV1.AvatarAddress => AvatarAddress;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var worldInformationAddress = AvatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = AvatarAddress.Derive(LegacyQuestListKey);
            var inventoryAddress = AvatarAddress.Derive(LegacyInventoryKey);
            var unlockedRecipeIdsAddress = AvatarAddress.Derive("recipe_ids");
            if (context.Rehearsal)
            {
                return states
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(AvatarAddress, MarkChanged)
                    .SetState(unlockedRecipeIdsAddress, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, context.Signer, Addresses.UnlockEquipmentRecipe);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}UnlockEquipmentRecipe exec started", addressesHex);
            if (!RecipeIds.Any() || RecipeIds.Any(i => i < 2))
            {
                throw new InvalidRecipeIdException();
            }

            WorldInformation worldInformation;
            bool migrationRequired = false;
            AvatarState avatarState = null;
            if (states.TryGetState(worldInformationAddress, out Dictionary rawInfo))
            {
                worldInformation = new WorldInformation(rawInfo);
            }
            else
            {
                // AvatarState migration required.
                if (states.TryGetAvatarState(context.Signer, AvatarAddress, out avatarState))
                {
                    worldInformation = avatarState.worldInformation;
                    migrationRequired = true;
                }
                else
                {
                    // Invalid Address.
                    throw new FailedLoadStateException($"Can't find AvatarState {AvatarAddress}");
                }
            }

            var equipmentRecipeSheet = states.GetSheet<EquipmentItemRecipeSheet>();

            var unlockedIds = UnlockedIds(states, unlockedRecipeIdsAddress, equipmentRecipeSheet, worldInformation, RecipeIds);

            FungibleAssetValue cost = CrystalCalculator.CalculateRecipeUnlockCost(RecipeIds, equipmentRecipeSheet);
            FungibleAssetValue balance = states.GetBalance(context.Signer, cost.Currency);

            if (balance < cost)
            {
                throw new NotEnoughFungibleAssetValueException($"required {cost}, but balance is {balance}");
            }

            if (migrationRequired)
            {
                states = states
                    .SetState(AvatarAddress, avatarState.SerializeV2())
                    .SetState(worldInformationAddress, worldInformation.Serialize())
                    .SetState(questListAddress, avatarState.questList.Serialize())
                    .SetState(inventoryAddress, avatarState.inventory.Serialize());
            }

            states = states.SetState(unlockedRecipeIdsAddress,
                    unlockedIds.Aggregate(List.Empty,
                        (current, address) => current.Add(address.Serialize())));
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}UnlockEquipmentRecipe Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states.TransferAsset(context.Signer, Addresses.UnlockEquipmentRecipe,  cost);
        }

        public static List<int> UnlockedIds(
            IAccountStateDelta states,
            Address unlockedRecipeIdsAddress,
            EquipmentItemRecipeSheet equipmentRecipeSheet,
            WorldInformation worldInformation,
            List<int> recipeIds
        )
        {
            List<int> unlockedIds = states.TryGetState(unlockedRecipeIdsAddress, out List rawIds)
                ? rawIds.ToList(StateExtensions.ToInteger)
                : new List<int>
                {
                    1
                };

            // Sort recipe by ItemSubType & UnlockStage.
            // 999 is not opened recipe.
            var sortedRecipeRows = equipmentRecipeSheet.Values
                .Where(r => r.UnlockStage != 999)
                .OrderBy(r => r.ItemSubType)
                .ThenBy(r => r.UnlockStage)
                .ToList();

            var unlockRecipeRows = sortedRecipeRows
                .Where(r => recipeIds.Contains(r.Id))
                .ToList();

            foreach (var recipeRow in unlockRecipeRows)
            {
                var recipeId = recipeRow.Id;
                if (unlockedIds.Contains(recipeId))
                {
                    // Already Unlocked
                    throw new AlreadyRecipeUnlockedException(
                        $"recipe: {recipeId} already unlocked.");
                }

                if (!worldInformation.IsStageCleared(recipeRow.UnlockStage))
                {
                    throw new NotEnoughClearedStageLevelException(
                        $"clear {recipeRow.UnlockStage} first.");
                }

                var index = sortedRecipeRows.IndexOf(recipeRow);
                if (index > 0)
                {
                    var prevRow = sortedRecipeRows[index - 1];
                    if (prevRow.ItemSubType == recipeRow.ItemSubType && !unlockedIds.Contains(prevRow.Id))
                    {
                        // Can't skip previous recipe unlock.
                        throw new InvalidRecipeIdException($"unlock {prevRow.Id} first.");
                    }
                }

                unlockedIds.Add(recipeId);
            }

            return unlockedIds;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["r"] = new List(RecipeIds.Select(i => i.Serialize())),
                ["a"] = AvatarAddress.Serialize(),
            }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            RecipeIds = plainValue["r"].ToList(StateExtensions.ToInteger);
            AvatarAddress = plainValue["a"].ToAddress();
        }
    }
}
