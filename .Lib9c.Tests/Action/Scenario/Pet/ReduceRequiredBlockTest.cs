// Reduces required blocks to craft item by ratio.
// ReducedBlock = Round(RequiredBlock * (100 - {ratio})) (ref. PetHelper.CalculateReducedBlockOnCraft)

namespace Lib9c.Tests.Action.Scenario.Pet
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Pet;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class ReduceRequiredBlockTest
    {
        private const PetOptionType PetOptionType =
            Nekoyume.Model.Pet.PetOptionType.ReduceRequiredBlock;

        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly IAccountStateDelta _initialStateV1;
        private readonly IAccountStateDelta _initialStateV2;
        private readonly Address _inventoryAddr;
        private readonly Address _worldInfoAddr;
        private readonly Address _recipeAddr;
        private int? _petId;

        public ReduceRequiredBlockTest()
        {
            (_tableSheets, _agentAddr, _avatarAddr, _initialStateV1, _initialStateV2)
                = InitializeUtil.InitializeStates();
            _inventoryAddr = _avatarAddr.Derive(LegacyInventoryKey);
            _worldInfoAddr = _avatarAddr.Derive(LegacyWorldInformationKey);
            _recipeAddr = _avatarAddr.Derive("recipe_ids");
        }

        [Theory]
        [InlineData(10114000, null)] // No Pet
        [InlineData(10114000, 1)] // Lv.1 reduces 5.5%
        [InlineData(10114000, 30)] // Lv.30 reduces 20%
        public void CombinationEquipmentTest(
            int targetItemId,
            int? petLevel
        )
        {
            var random = new TestRandom();

            // Get Recipe
            var recipe = _tableSheets.EquipmentItemRecipeSheet.OrderedList.First(
                recipe => recipe.ResultEquipmentId == targetItemId);
            Assert.NotNull(recipe);
            var expectedBlock = recipe.RequiredBlockIndex;

            // Get Materials and stages
            List<EquipmentItemSubRecipeSheet.MaterialInfo> materialList =
                recipe.GetAllMaterials(_tableSheets.EquipmentItemSubRecipeSheetV2).ToList();
            var stageList = List.Empty;
            for (var i = 1; i < recipe.UnlockStage + 1; i++)
            {
                stageList = stageList.Add(i.Serialize());
            }

            var stateV2 = _initialStateV2.SetState(_recipeAddr, stageList);

            // Get pet
            if (!(petLevel is null))
            {
                var petRow = _tableSheets.PetOptionSheet.Values.First(
                    pet => pet.LevelOptionMap[(int)petLevel!].OptionType == PetOptionType
                );

                _petId = petRow.PetId;
                stateV2 = stateV2.SetState(
                    PetState.DeriveAddress(_avatarAddr, (int)_petId),
                    new List(_petId!.Serialize(), petLevel.Serialize(), 0L.Serialize())
                );
                expectedBlock = (long)Math.Round(
                    expectedBlock * (1 - petRow.LevelOptionMap[(int)petLevel].OptionValue / 100)
                );
            }

            // Prepare
            stateV2 = CraftUtil.PrepareCombinationSlot(stateV2, _avatarAddr, 0);
            stateV2 = CraftUtil.AddMaterialsToInventory(
                stateV2,
                _tableSheets,
                _avatarAddr,
                materialList,
                random
            );
            stateV2 = CraftUtil.UnlockStage(
                stateV2,
                _tableSheets,
                _worldInfoAddr,
                recipe.UnlockStage
            );

            // Do Combination
            var action = new CombinationEquipment
            {
                avatarAddress = _avatarAddr,
                slotIndex = 0,
                recipeId = recipe.Id,
                subRecipeId = recipe.SubRecipeIds?[0],
                petId = _petId,
            };

            stateV2 = action.Execute(new ActionContext
            {
                PreviousState = stateV2,
                Signer = _agentAddr,
                BlockIndex = 0L,
                Random = random,
            });

            var slotState = stateV2.GetCombinationSlotState(_avatarAddr, 0);
            // TEST: RequiredBlockIndex
            Assert.Equal(expectedBlock, slotState.RequiredBlockIndex);
        }
    }
}
