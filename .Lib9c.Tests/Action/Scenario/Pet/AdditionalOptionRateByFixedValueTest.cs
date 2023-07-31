// Increases item option success ratio for each options independent

namespace Lib9c.Tests.Action.Scenario.Pet
{
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Pet;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class AdditionalOptionRateByFixedValueTest
    {
        private const PetOptionType PetOptionType
            = Nekoyume.Model.Pet.PetOptionType.AdditionalOptionRateByFixedValue;

        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly IAccountStateDelta _initialStateV1;
        private readonly IAccountStateDelta _initialStateV2;
        private readonly Address _inventoryAddr;
        private readonly Address _worldInfoAddr;
        private readonly Address _recipeAddr;
        private int? _petId;

        public AdditionalOptionRateByFixedValueTest()
        {
            (_tableSheets, _agentAddr, _avatarAddr, _initialStateV1, _initialStateV2)
                = InitializeUtil.InitializeStates();
            _inventoryAddr = _avatarAddr.Derive(LegacyInventoryKey);
            _worldInfoAddr = _avatarAddr.Derive(LegacyWorldInformationKey);
            _recipeAddr = _avatarAddr.Derive("recipe_ids");
        }

        [Theory]
        [InlineData(73, 10114000, 1)]
        [InlineData(37, 10114000, 30)]
        public void CombinationEquipmentTest(
            int randomSeed,
            int targetItemId,
            int petLevel
        )
        {
            var (beforeResult, afterResult) = (false, false);
            // Get Recipe
            var recipe = _tableSheets.EquipmentItemRecipeSheet.Values.First(
                recipe => recipe.ResultEquipmentId == targetItemId
            );
            Assert.NotNull(recipe);

            // Get Materials and stages
            List<EquipmentItemSubRecipeSheet.MaterialInfo> materialList =
                recipe.GetAllMaterials(
                    _tableSheets.EquipmentItemSubRecipeSheetV2, CraftType.Premium
                ).ToList();
            var stageList = List.Empty;
            for (var i = 0; i < recipe.UnlockStage; i++)
            {
                stageList = stageList.Add(i.Serialize());
            }

            var stateV2 = _initialStateV2.SetState(_recipeAddr, stageList);
            stateV2 = CraftUtil.UnlockStage(
                stateV2,
                _tableSheets,
                _worldInfoAddr,
                recipe.UnlockStage
            );

            var subRecipe = _tableSheets.EquipmentItemSubRecipeSheetV2[recipe.SubRecipeIds![1]];
            var (originalOption2Ratio, originalOption3Ratio, originalOption4Ratio) =
                (subRecipe.Options[1].Ratio, subRecipe.Options[2].Ratio,
                    subRecipe.Options[3].Ratio);
            var (expectedOption2Ratio, expectedOption3Ratio, expectedOption4Ratio) =
                (originalOption2Ratio, originalOption3Ratio, originalOption4Ratio);

            // Get pet
            var petRow = _tableSheets.PetOptionSheet.Values.First(
                pet => pet.LevelOptionMap[(int)petLevel!].OptionType == PetOptionType
            );
            _petId = petRow.PetId;
            stateV2 = stateV2.SetState(
                PetState.DeriveAddress(_avatarAddr, (int)_petId),
                new List(_petId!.Serialize(), petLevel.Serialize(), 0L.Serialize())
            );
            var increment = (int)petRow.LevelOptionMap[petLevel].OptionValue * 100;
            (expectedOption2Ratio, expectedOption3Ratio, expectedOption4Ratio) =
            (
                originalOption2Ratio + increment,
                originalOption3Ratio + increment,
                originalOption4Ratio + increment
            );

            // Prepare
            stateV2 = CraftUtil.PrepareCombinationSlot(stateV2, _avatarAddr, 0);
            stateV2 = CraftUtil.PrepareCombinationSlot(stateV2, _avatarAddr, 1);

            // Find specific random seed to meet test condition
            var random = new TestRandom(randomSeed);

            // Give Materials
            stateV2 = CraftUtil.AddMaterialsToInventory(
                stateV2,
                _tableSheets,
                _avatarAddr,
                materialList,
                random
            );

            // Do combination without pet
            var action = new CombinationEquipment
            {
                avatarAddress = _avatarAddr,
                slotIndex = 0,
                recipeId = recipe.Id,
                subRecipeId = recipe.SubRecipeIds?[1],
            };
            stateV2 = action.Execute(new ActionContext
            {
                PreviousState = stateV2,
                Signer = _agentAddr,
                BlockIndex = 0L,
                Random = random,
            });
            var slotState = stateV2.GetCombinationSlotState(_avatarAddr, 0);
            // TEST: No additional option added (1 star)
            Assert.Equal(
                recipe.RequiredBlockIndex + subRecipe.RequiredBlockIndex +
                subRecipe.Options[0].RequiredBlockIndex,
                slotState.RequiredBlockIndex
            );

            /*
             * After this line, we reset random seed and retry combination "with" pet.
             * This should be success to add failed option before
             */

            // Reset Random
            random = new TestRandom(randomSeed);

            // Give materials
            stateV2 = CraftUtil.AddMaterialsToInventory(
                stateV2,
                _tableSheets,
                _avatarAddr,
                materialList,
                random
            );

            var petAction = new CombinationEquipment
            {
                avatarAddress = _avatarAddr,
                slotIndex = 1,
                recipeId = recipe.Id,
                subRecipeId = recipe.SubRecipeIds?[1],
                petId = _petId,
            };
            stateV2 = petAction.Execute(new ActionContext
            {
                PreviousState = stateV2,
                Signer = _agentAddr,
                BlockIndex = 0L,
                Random = random,
            });
            var petSlotState = stateV2.GetCombinationSlotState(_avatarAddr, 1);
            // TEST: One additional option added (2 star)
            Assert.Equal(
                recipe.RequiredBlockIndex + subRecipe.RequiredBlockIndex +
                subRecipe.Options[0].RequiredBlockIndex +
                subRecipe.Options[1].RequiredBlockIndex,
                petSlotState.RequiredBlockIndex
            );
        }
    }
}
