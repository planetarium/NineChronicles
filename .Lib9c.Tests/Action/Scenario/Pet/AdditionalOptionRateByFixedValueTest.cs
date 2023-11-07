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
    using Nekoyume.TableData.Pet;
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
        private readonly IAccount _initialStateV1;
        private readonly IAccount _initialStateV2;
        private readonly Address _inventoryAddr;
        private readonly Address _worldInfoAddr;
        private readonly Address _recipeAddr;
        private int? _petId;

        public AdditionalOptionRateByFixedValueTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            sheets[nameof(PetOptionSheet)] = @"ID,_PET NAME,PetLevel,OptionType,OptionValue
1001,D:CC 블랙캣,1,ReduceRequiredBlock,5.5
1001,D:CC 블랙캣,2,ReduceRequiredBlock,6
1001,D:CC 블랙캣,3,ReduceRequiredBlock,6.5
1001,D:CC 블랙캣,4,ReduceRequiredBlock,7
1001,D:CC 블랙캣,5,ReduceRequiredBlock,7.5
1001,D:CC 블랙캣,6,ReduceRequiredBlock,8
1001,D:CC 블랙캣,7,ReduceRequiredBlock,8.5
1001,D:CC 블랙캣,8,ReduceRequiredBlock,9
1001,D:CC 블랙캣,9,ReduceRequiredBlock,9.5
1001,D:CC 블랙캣,10,ReduceRequiredBlock,10
1001,D:CC 블랙캣,11,ReduceRequiredBlock,10.5
1001,D:CC 블랙캣,12,ReduceRequiredBlock,11
1001,D:CC 블랙캣,13,ReduceRequiredBlock,11.5
1001,D:CC 블랙캣,14,ReduceRequiredBlock,12
1001,D:CC 블랙캣,15,ReduceRequiredBlock,12.5
1001,D:CC 블랙캣,16,ReduceRequiredBlock,13
1001,D:CC 블랙캣,17,ReduceRequiredBlock,13.5
1001,D:CC 블랙캣,18,ReduceRequiredBlock,14
1001,D:CC 블랙캣,19,ReduceRequiredBlock,14.5
1001,D:CC 블랙캣,20,ReduceRequiredBlock,15
1001,D:CC 블랙캣,21,ReduceRequiredBlock,15.5
1001,D:CC 블랙캣,22,ReduceRequiredBlock,16
1001,D:CC 블랙캣,23,ReduceRequiredBlock,16.5
1001,D:CC 블랙캣,24,ReduceRequiredBlock,17
1001,D:CC 블랙캣,25,ReduceRequiredBlock,17.5
1001,D:CC 블랙캣,26,ReduceRequiredBlock,18
1001,D:CC 블랙캣,27,ReduceRequiredBlock,18.5
1001,D:CC 블랙캣,28,ReduceRequiredBlock,19
1001,D:CC 블랙캣,29,ReduceRequiredBlock,19.5
1001,D:CC 블랙캣,30,ReduceRequiredBlock,20
1002,빨간 동글이,1,DiscountMaterialCostCrystal,2.5
1002,빨간 동글이,2,DiscountMaterialCostCrystal,3
1002,빨간 동글이,3,DiscountMaterialCostCrystal,3.5
1002,빨간 동글이,4,DiscountMaterialCostCrystal,4
1002,빨간 동글이,5,DiscountMaterialCostCrystal,4.5
1002,빨간 동글이,6,DiscountMaterialCostCrystal,5
1002,빨간 동글이,7,DiscountMaterialCostCrystal,5.5
1002,빨간 동글이,8,DiscountMaterialCostCrystal,6
1002,빨간 동글이,9,DiscountMaterialCostCrystal,6.5
1002,빨간 동글이,10,DiscountMaterialCostCrystal,7
1002,빨간 동글이,11,DiscountMaterialCostCrystal,7.5
1002,빨간 동글이,12,DiscountMaterialCostCrystal,8
1002,빨간 동글이,13,DiscountMaterialCostCrystal,8.5
1002,빨간 동글이,14,DiscountMaterialCostCrystal,9
1002,빨간 동글이,15,DiscountMaterialCostCrystal,9.5
1002,빨간 동글이,16,DiscountMaterialCostCrystal,10
1002,빨간 동글이,17,DiscountMaterialCostCrystal,10.5
1002,빨간 동글이,18,DiscountMaterialCostCrystal,11
1002,빨간 동글이,19,DiscountMaterialCostCrystal,11.5
1002,빨간 동글이,20,DiscountMaterialCostCrystal,12
1002,빨간 동글이,21,DiscountMaterialCostCrystal,12.5
1002,빨간 동글이,22,DiscountMaterialCostCrystal,13
1002,빨간 동글이,23,DiscountMaterialCostCrystal,13.5
1002,빨간 동글이,24,DiscountMaterialCostCrystal,14
1002,빨간 동글이,25,DiscountMaterialCostCrystal,14.5
1002,빨간 동글이,26,DiscountMaterialCostCrystal,15
1002,빨간 동글이,27,DiscountMaterialCostCrystal,15.5
1002,빨간 동글이,28,DiscountMaterialCostCrystal,16
1002,빨간 동글이,29,DiscountMaterialCostCrystal,16.5
1002,빨간 동글이,30,DiscountMaterialCostCrystal,17
1003,빛의 발키리,1,IncreaseBlockPerHourglass,1
1003,빛의 발키리,2,IncreaseBlockPerHourglass,2
1003,빛의 발키리,3,IncreaseBlockPerHourglass,3
1003,빛의 발키리,4,IncreaseBlockPerHourglass,4
1003,빛의 발키리,5,IncreaseBlockPerHourglass,5
1003,빛의 발키리,6,IncreaseBlockPerHourglass,6
1003,빛의 발키리,7,IncreaseBlockPerHourglass,7
1003,빛의 발키리,8,IncreaseBlockPerHourglass,8
1003,빛의 발키리,9,IncreaseBlockPerHourglass,9
1003,빛의 발키리,10,IncreaseBlockPerHourglass,10
1003,빛의 발키리,11,IncreaseBlockPerHourglass,11
1003,빛의 발키리,12,IncreaseBlockPerHourglass,12
1003,빛의 발키리,13,IncreaseBlockPerHourglass,13
1003,빛의 발키리,14,IncreaseBlockPerHourglass,14
1003,빛의 발키리,15,IncreaseBlockPerHourglass,15
1003,빛의 발키리,16,IncreaseBlockPerHourglass,16
1003,빛의 발키리,17,IncreaseBlockPerHourglass,17
1003,빛의 발키리,18,IncreaseBlockPerHourglass,18
1003,빛의 발키리,19,IncreaseBlockPerHourglass,19
1003,빛의 발키리,20,IncreaseBlockPerHourglass,20
1003,빛의 발키리,21,IncreaseBlockPerHourglass,21
1003,빛의 발키리,22,IncreaseBlockPerHourglass,22
1003,빛의 발키리,23,IncreaseBlockPerHourglass,23
1003,빛의 발키리,24,IncreaseBlockPerHourglass,24
1003,빛의 발키리,25,IncreaseBlockPerHourglass,25
1003,빛의 발키리,26,IncreaseBlockPerHourglass,26
1003,빛의 발키리,27,IncreaseBlockPerHourglass,27
1003,빛의 발키리,28,IncreaseBlockPerHourglass,28
1003,빛의 발키리,29,IncreaseBlockPerHourglass,29
1003,빛의 발키리,30,IncreaseBlockPerHourglass,30
1004,꼬마 펜리르,1,AdditionalOptionRateByFixedValue,5.5
1004,꼬마 펜리르,2,AdditionalOptionRateByFixedValue,6
1004,꼬마 펜리르,3,AdditionalOptionRateByFixedValue,6.5
1004,꼬마 펜리르,4,AdditionalOptionRateByFixedValue,7
1004,꼬마 펜리르,5,AdditionalOptionRateByFixedValue,7.5
1004,꼬마 펜리르,6,AdditionalOptionRateByFixedValue,8
1004,꼬마 펜리르,7,AdditionalOptionRateByFixedValue,8.5
1004,꼬마 펜리르,8,AdditionalOptionRateByFixedValue,9
1004,꼬마 펜리르,9,AdditionalOptionRateByFixedValue,9.5
1004,꼬마 펜리르,10,AdditionalOptionRateByFixedValue,10
1004,꼬마 펜리르,11,AdditionalOptionRateByFixedValue,10.5
1004,꼬마 펜리르,12,AdditionalOptionRateByFixedValue,11
1004,꼬마 펜리르,13,AdditionalOptionRateByFixedValue,11.5
1004,꼬마 펜리르,14,AdditionalOptionRateByFixedValue,12
1004,꼬마 펜리르,15,AdditionalOptionRateByFixedValue,12.5
1004,꼬마 펜리르,16,AdditionalOptionRateByFixedValue,13
1004,꼬마 펜리르,17,AdditionalOptionRateByFixedValue,13.5
1004,꼬마 펜리르,18,AdditionalOptionRateByFixedValue,14
1004,꼬마 펜리르,19,AdditionalOptionRateByFixedValue,14.5
1004,꼬마 펜리르,20,AdditionalOptionRateByFixedValue,15
1004,꼬마 펜리르,21,AdditionalOptionRateByFixedValue,15.5
1004,꼬마 펜리르,22,AdditionalOptionRateByFixedValue,16
1004,꼬마 펜리르,23,AdditionalOptionRateByFixedValue,16.5
1004,꼬마 펜리르,24,AdditionalOptionRateByFixedValue,17
1004,꼬마 펜리르,25,AdditionalOptionRateByFixedValue,17.5
1004,꼬마 펜리르,26,AdditionalOptionRateByFixedValue,18
1004,꼬마 펜리르,27,AdditionalOptionRateByFixedValue,18.5
1004,꼬마 펜리르,28,AdditionalOptionRateByFixedValue,19
1004,꼬마 펜리르,29,AdditionalOptionRateByFixedValue,19.5
1004,꼬마 펜리르,30,AdditionalOptionRateByFixedValue,20";
            (_tableSheets, _agentAddr, _avatarAddr, _initialStateV1, _initialStateV2)
                = InitializeUtil.InitializeStates(sheetsOverride: sheets);
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
            var action = new CombinationEquipment16
            {
                avatarAddress = _avatarAddr,
                slotIndex = 0,
                recipeId = recipe.Id,
                subRecipeId = recipe.SubRecipeIds?[1],
            };
            var ctx = new ActionContext
            {
                PreviousState = stateV2,
                Signer = _agentAddr,
                BlockIndex = 0L,
            };
            ctx.SetRandom(random);
            stateV2 = action.Execute(ctx);
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

            var petAction = new CombinationEquipment16
            {
                avatarAddress = _avatarAddr,
                slotIndex = 1,
                recipeId = recipe.Id,
                subRecipeId = recipe.SubRecipeIds?[1],
                petId = _petId,
            };
            ctx = new ActionContext
            {
                PreviousState = stateV2,
                Signer = _agentAddr,
                BlockIndex = 0L,
            };
            ctx.SetRandom(random);
            stateV2 = petAction.Execute(ctx);
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
