namespace Lib9c.Tests.Action.Scenario.Pet
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.Pet;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class CommonTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly Address _recipeAddr;
        private readonly Address _worldInfoAddr;
        private readonly IAccountStateDelta _initialStateV1;
        private readonly IAccountStateDelta _initialStateV2;

        public CommonTest()
        {
            (_tableSheets, _agentAddr, _avatarAddr, _initialStateV1, _initialStateV2) =
                InitializeUtil.InitializeStates();
            _recipeAddr = _avatarAddr.Derive("recipe_ids");
            _worldInfoAddr = _avatarAddr.Derive(LegacyWorldInformationKey);
        }

        // Pet level range test (1~30)
        [Theory]
        [InlineData(0)] // Min. level of pet is 1
        [InlineData(31)] // Max. level of pet is 30
        public void PetLevelRangeTest(int petLevel)
        {
            foreach (var petOptionType in Enum.GetValues<PetOptionType>())
            {
                Assert.Throws<KeyNotFoundException>(
                    () => _tableSheets.PetOptionSheet.Values.First(
                        pet => pet.LevelOptionMap[petLevel].OptionType == petOptionType
                    )
                );
            }
        }

        // You cannot use one pet to the multiple slots at the same time
        [Fact]
        public void PetCannotBeUsedToTwoSlotsAtTheSameTime()
        {
            const int itemId = 10114000;
            const int petId = 1;
            const int petLevel = 1;

            var random = new TestRandom();

            // Get Pet
            var stateV2 = _initialStateV2.SetState(
                PetState.DeriveAddress(_avatarAddr, petId),
                new List(petId.Serialize(), petLevel.Serialize(), 0L.Serialize())
            );

            // Get Recipe
            var recipe = _tableSheets.EquipmentItemRecipeSheet.Values.First(
                recipe => recipe.ResultEquipmentId == itemId
            );
            List<EquipmentItemSubRecipeSheet.MaterialInfo> materialList =
                recipe.GetAllMaterials(_tableSheets.EquipmentItemSubRecipeSheetV2).ToList();
            var stageList = List.Empty;
            for (var i = 0; i < recipe.UnlockStage; i++)
            {
                stageList = stageList.Add(i.Serialize());
            }

            stateV2 = stateV2.SetState(_recipeAddr, stageList);
            stateV2 = CraftUtil.UnlockStage(
                stateV2,
                _tableSheets,
                _worldInfoAddr,
                recipe.UnlockStage
            );

            // Prepare Slots
            stateV2 = CraftUtil.PrepareCombinationSlot(stateV2, _avatarAddr, 0);
            stateV2 = CraftUtil.PrepareCombinationSlot(stateV2, _avatarAddr, 1);

            stateV2 = CraftUtil.AddMaterialsToInventory(
                stateV2,
                _tableSheets,
                _avatarAddr,
                materialList,
                random
            );
            stateV2 = CraftUtil.AddMaterialsToInventory(
                stateV2,
                _tableSheets,
                _avatarAddr,
                materialList,
                random
            );

            // Combination1
            var action1 = new CombinationEquipment
            {
                avatarAddress = _avatarAddr,
                slotIndex = 0,
                recipeId = recipe.Id,
                subRecipeId = recipe.SubRecipeIds?[0],
                petId = petId,
            };
            stateV2 = action1.Execute(new ActionContext
            {
                PreviousState = stateV2,
                Signer = _agentAddr,
                BlockIndex = 0L,
                Random = random,
            });

            // Combination2: Raises error
            var action2 = new CombinationEquipment
            {
                avatarAddress = _avatarAddr,
                slotIndex = 1,
                recipeId = recipe.Id,
                subRecipeId = recipe.SubRecipeIds?[0],
                petId = petId,
            };
            Assert.Throws<PetIsLockedException>(() => action2.Execute(
                new ActionContext
                {
                    PreviousState = stateV2,
                    Signer = _agentAddr,
                    BlockIndex = 1L,
                    Random = random,
                })
            );
        }
    }
}
