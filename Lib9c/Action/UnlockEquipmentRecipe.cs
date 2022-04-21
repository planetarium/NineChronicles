using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("unlock_equipment_recipe")]
    public class UnlockEquipmentRecipe: GameAction
    {
        public List<int> RecipeIds = new List<int>();
        public Address AvatarAddress;

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
                    .MarkBalanceChanged(GoldCurrencyMock, AvatarAddress);
            }

            if (RecipeIds.Any(i => i < 2))
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

            List<int> unlockedIds = states.TryGetState(unlockedRecipeIdsAddress, out List rawIds)
                ? rawIds.ToList(StateExtensions.ToInteger)
                : new List<int>
                {
                    1
                };

            var sortedRecipeIds = RecipeIds.OrderBy(i => i).ToList();
            foreach (var recipeId in sortedRecipeIds)
            {
                if (unlockedIds.Contains(recipeId))
                {
                    // Already Unlocked
                    throw new AlreadyRecipeUnlockedException($"recipe: {recipeId} already unlocked.");
                }

                var prevId = recipeId - 1;
                if (!unlockedIds.Contains(prevId))
                {
                    // Can't skip previous recipe unlock.
                    throw new InvalidRecipeIdException($"unlock {prevId} first.");
                }

                unlockedIds.Add(recipeId);

                EquipmentItemRecipeSheet.Row row = equipmentRecipeSheet[recipeId];
                if (!worldInformation.IsStageCleared(row.UnlockStage))
                {
                    throw new NotEnoughClearedStageLevelException($"clear {row.UnlockStage} first.");
                }
            }

            FungibleAssetValue cost = CrystalCalculator.CalculateRecipeUnlockCost(sortedRecipeIds, equipmentRecipeSheet);
            FungibleAssetValue balance = states.GetBalance(AvatarAddress, cost.Currency);

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
            return states.BurnAsset(AvatarAddress, cost);
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
