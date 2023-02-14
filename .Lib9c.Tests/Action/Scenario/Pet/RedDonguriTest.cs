// RedDongle reduces required crystal to craft item by ratio.

namespace Lib9c.Tests.Action.Scenario.Pet
{
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Util;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class RedDonguriTest
    {
        private const int PetId = 2; // RedDonguri
        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly Address _inventoryAddr;
        private readonly Address _worldInformationAddr;
        private readonly IAccountStateDelta _initialStateV2;
        private readonly TableSheets _tableSheets;

        public RedDonguriTest()
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
        [InlineData(0, 10114000, null, 9540)]
        [InlineData(0, 10114000, 0, 9540)]
        [InlineData(0, 10114000, 1, 9302)] // RedDonguri lv.1 reduces 2.5% of crystal: 9540 -> 9301.5
        [InlineData(0, 10114000, 30, 7918)] // RedDonguri lv.30 reduces 17& of crystal: 9540 -> 7918.2
        public void CraftEquipment_WithRedDonguri(
            int randomSeed,
            int targetItemId,
            int? petLevel,
            int expectedCrystal
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

            var stateV2 = _initialStateV2.SetState(
                _avatarAddr.Derive("recipe_ids"),
                stageList
            );

            // Get pet
            if (!(petLevel is null))
            {
                stateV2 = stateV2.SetState(
                    PetState.DeriveAddress(_avatarAddr, PetId),
                    new List(PetId.Serialize(), petLevel.Serialize(), 0L.Serialize()));
            }

            // Prepare
            stateV2 = CraftUtil.PrepareCombinationSlot(stateV2, _avatarAddr, 0);
            stateV2 = CurrencyUtil.AddCurrency(stateV2, _agentAddr, crystal, expectedCrystal);
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
                petId = petLevel is null ? (int?)null : PetId,
                payByCrystal = true,
            };

            stateV2 = action.Execute(new ActionContext
            {
                PreviousStates = stateV2,
                Signer = _agentAddr,
                BlockIndex = 0L,
                Random = random,
            });

            // TEST: All given crystals are used
            Assert.Equal(0 * crystal, stateV2.GetBalance(_agentAddr, crystal));
        }
    }
}
