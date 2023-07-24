// Reduces required crystal to craft item by ratio.

namespace Lib9c.Tests.Action.Scenario.Pet
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Bencodex.Types;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Model.Pet;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class DiscountMaterialCostCrystalTest
    {
        private const PetOptionType PetOptionType =
            Nekoyume.Model.Pet.PetOptionType.DiscountMaterialCostCrystal;

        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly Address _inventoryAddr;
        private readonly Address _worldInformationAddr;
        private readonly IAccountStateDelta _initialStateV2;
        private readonly TableSheets _tableSheets;
        private int? _petId;

        public DiscountMaterialCostCrystalTest()
        {
            (
                _tableSheets,
                _agentAddr,
                _avatarAddr,
                _,
                _initialStateV2
            ) = InitializeUtil.InitializeStates();
            _inventoryAddr = _avatarAddr.Derive(LegacyInventoryKey);
            _worldInformationAddr = _avatarAddr.Derive(LegacyWorldInformationKey);
        }

        [Theory]
        [InlineData(0, 10114000, null)]
        [InlineData(0, 10114000, 1)] // Lv.1 reduces 2.5% of crystal
        [InlineData(0, 10114000, 30)] // Lv.30 reduces 17& of crystal
        public void CraftEquipmentTest(
            int randomSeed,
            int targetItemId,
            int? petLevel
        )
        {
            var crystal = Currency.Legacy("CRYSTAL", 18, null);
            var random = new TestRandom(randomSeed);

            // Get recipe
            var recipe =
                _tableSheets.EquipmentItemRecipeSheet.OrderedList.First(
                    recipe => recipe.ResultEquipmentId == targetItemId
                );
            Assert.NotNull(recipe);

            // Get materials and stages
            List<EquipmentItemSubRecipeSheet.MaterialInfo> materialList =
                recipe.GetAllMaterials(_tableSheets.EquipmentItemSubRecipeSheetV2).ToList();
            var stageList = List.Empty;
            for (var i = 1; i < recipe.UnlockStage + 1; i++)
            {
                stageList = stageList.Add(i.Serialize());
            }

            var context = new ActionContext();
            var stateV2 = _initialStateV2.SetState(
                _avatarAddr.Derive("recipe_ids"),
                stageList
            );
            var expectedCrystal = 0 * crystal;
            foreach (var material in materialList)
            {
                var materialCost = _tableSheets.CrystalMaterialCostSheet.Values.First(
                    m => m.ItemId == material.Id
                );
                expectedCrystal += material.Count * materialCost.CRYSTAL * crystal;
            }

            // Get pet
            if (!(petLevel is null))
            {
                var petRow = _tableSheets.PetOptionSheet.Values.First(
                    pet => pet.LevelOptionMap[(int)petLevel].OptionType == PetOptionType
                );

                _petId = petRow.PetId;
                stateV2 = stateV2.SetState(
                    PetState.DeriveAddress(_avatarAddr, (int)_petId),
                    new List(_petId.Serialize(), petLevel.Serialize(), 0L.Serialize()));
                expectedCrystal *= (BigInteger)(
                    10000 - petRow.LevelOptionMap[(int)petLevel].OptionValue * 100
                );
                expectedCrystal = expectedCrystal.DivRem(10000, out _);
            }

            // Prepare
            stateV2 = CraftUtil.PrepareCombinationSlot(stateV2, _avatarAddr, 0);
            stateV2 = CurrencyUtil.AddCurrency(context, stateV2, _agentAddr, crystal, expectedCrystal);
            stateV2 = CraftUtil.UnlockStage(
                stateV2,
                _tableSheets,
                _worldInformationAddr,
                recipe.UnlockStage
            );

            // Do combination
            var action = new CombinationEquipment
            {
                avatarAddress = _avatarAddr,
                slotIndex = 0,
                recipeId = recipe.Id,
                subRecipeId = recipe.SubRecipeIds?[0],
                petId = _petId,
                payByCrystal = true,
            };

            stateV2 = action.Execute(new ActionContext
            {
                PreviousState = stateV2,
                Signer = _agentAddr,
                BlockIndex = 0L,
                Random = random,
            });

            // TEST: All given crystals are used
            Assert.Equal(0 * crystal, stateV2.GetBalance(_agentAddr, crystal));
        }
    }
}
