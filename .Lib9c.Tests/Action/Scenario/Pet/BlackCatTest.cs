// BlackCat reduces required blocks to craft item by ratio.
// ReducedBlock = Round(RequiredBlock * (100 - {ratio})) (ref. PetHelper.CalculateReducedBlockOnCraft)

namespace Lib9c.Tests.Action.Scenario.Pet
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Util;
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class BlackCatTest
    {
        private const int PetId = 1; // BlackCat
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly IAccountStateDelta _initialStateV1;
        private readonly IAccountStateDelta _initialStateV2;
        private readonly Address _inventoryAddr;
        private readonly Address _worldInfoAddr;
        private readonly Address _recipeAddr;

        public BlackCatTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            (_tableSheets, _agentAddr, _avatarAddr, _initialStateV1, _initialStateV2)
                = InitializeUtil.InitializeStates();
            _inventoryAddr = _avatarAddr.Derive(LegacyInventoryKey);
            _worldInfoAddr = _avatarAddr.Derive(LegacyWorldInformationKey);
            _recipeAddr = _avatarAddr.Derive("recipe_ids");
        }

        [Theory]
        [InlineData(10114000, null, 477)] // No Pet
        [InlineData(10114000, 0, 477)] // Pet lv.0 means no pet
        [InlineData(10114000, 1, 451)] // Black cat lv.1 reduces 5.5%: 477 -> 450.765
        [InlineData(10114000, 30, 382)] // Black cat lv.30 reduces 20%: 477 -> 381.6
        public void CombinationEquipment_WithBlackCat(
            int targetItemId,
            int? petLevel,
            long expectedBlock
        )
        {
            var random = new TestRandom();

            // Get Recipe
            var recipe = _tableSheets.EquipmentItemRecipeSheet.OrderedList.First(
                recipe => recipe.ResultEquipmentId == targetItemId);
            Assert.NotNull(recipe);

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
                stateV2 = stateV2.SetState(
                    PetState.DeriveAddress(_avatarAddr, PetId),
                    new List(PetId.Serialize(), petLevel.Serialize(), 0L.Serialize()));
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
                petId = petLevel is null ? (int?)null : PetId,
            };

            stateV2 = action.Execute(new ActionContext
            {
                PreviousStates = stateV2,
                Signer = _agentAddr,
                BlockIndex = 0L,
                Random = random,
            });

            var slotState = stateV2.GetCombinationSlotState(_avatarAddr, 0);
            // TEST: RequiredBlockIndex
            Assert.Equal(expectedBlock, slotState.RequiredBlockIndex);
        }

        [Theory]
        [InlineData(0, 10114000, null, 25)]
        [InlineData(0, 10114000, 0, 25)]
        [InlineData(0, 10114000, 1, 24)] // Black cat lv.1 reduces 5.5%: 25 -> 23.625
        [InlineData(0, 10114000, 30, 20)] // Black cat lv.30 reduces 20%: 25 -> 20
        [InlineData(1, 10114000, 0, 31)]
        [InlineData(1, 10114000, 1, 29)] // Black cat lv.1 reduces 5.5%: 31 -> 29.295
        [InlineData(1, 10114000, 30, 25)] // Black cat lv.30 reduces 20%: 31 -> 24.8
        public void ItemEnhancement_WithBlackCat(
            int randomSeed,
            int targetItemId,
            int? petLevel,
            long expectedBlock
        )
        {
            var avatarState = _initialStateV2.GetAvatarStateV2(_avatarAddr);
            var random = new TestRandom(randomSeed);
            var equipmentRow =
                _tableSheets.EquipmentItemSheet.Values.First(e => e.Id == targetItemId);
            var equipment = (Equipment)ItemFactory.CreateItemUsable(equipmentRow, default, 0);
            var material = (Equipment)ItemFactory.CreateItemUsable(
                equipmentRow,
                Guid.NewGuid(),
                0);

            // Give equipments
            avatarState.inventory.AddItem(equipment);
            avatarState.inventory.AddItem(material);
            var stateV2 =
                _initialStateV2.SetState(_inventoryAddr, avatarState.inventory.Serialize());

            // Get pet
            if (!(petLevel is null))
            {
                stateV2 = stateV2.SetState(
                    PetState.DeriveAddress(_avatarAddr, PetId),
                    new List(PetId.Serialize(), petLevel.Serialize(), 0L.Serialize()));
            }

            // Prepare
            stateV2 = CraftUtil.UnlockStage(
                stateV2,
                _tableSheets,
                _worldInfoAddr,
                GameConfig.RequireClearedStageLevel.ItemEnhancementAction
            );
            stateV2 = CraftUtil.PrepareCombinationSlot(stateV2, _avatarAddr, 0);

            // Do Enhancement
            var action = new ItemEnhancement
            {
                avatarAddress = _avatarAddr,
                itemId = equipment.ItemId,
                materialId = material.ItemId,
                slotIndex = 0,
                petId = petLevel is null ? (int?)null : PetId,
            };

            stateV2 = action.Execute(new ActionContext
            {
                PreviousStates = stateV2,
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
