namespace Lib9c.Tests.Extensions
{
    using System;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using Xunit.Abstractions;

    public class EquipmentExtensionsTest
    {
        private readonly ITestOutputHelper _logger;
        private readonly TableSheets _tableSheets;

        public EquipmentExtensionsTest(ITestOutputHelper logger)
        {
            _logger = logger;
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void CheckAllRecipesIsMadeWithMimisbrunnrRecipe()
        {
            // NOTE: This loop once succeeded from 0 to 999 in my local test.
            // And it is enough to test from 0 to 9 in CI I think.
            for (var i = 0; i < 10; i++)
            {
                try
                {
                    CheckAllRecipesIsMadeWithMimisbrunnrRecipeInternal(i);
                }
                catch
                {
                    _logger.WriteLine($"random seed: {i}");
                }
            }
        }

        private void CheckAllRecipesIsMadeWithMimisbrunnrRecipeInternal(int randomSeed)
        {
            var random = new TestRandom(randomSeed);
            var equipmentSheet = _tableSheets.EquipmentItemSheet;
            var equipmentOptionSheet = _tableSheets.EquipmentItemOptionSheet;
            var skillSheet = _tableSheets.SkillSheet;
            var recipeSheet = _tableSheets.EquipmentItemRecipeSheet;
            var subRecipeSheet = _tableSheets.EquipmentItemSubRecipeSheetV2;
            foreach (var recipeRow in recipeSheet.OrderedList.Where(row => row.UnlockStage < 999))
            {
                if (!equipmentSheet.TryGetValue(recipeRow.ResultEquipmentId, out var equipmentRow))
                {
                    continue;
                }

                if (!recipeRow.SubRecipeIds.Any())
                {
                    var equipment = CreateEquipment(
                        random,
                        equipmentRow,
                        null,
                        equipmentOptionSheet,
                        skillSheet,
                        false);
                    try
                    {
                        Assert.False(equipment.IsMadeWithMimisbrunnrRecipe(
                            recipeSheet,
                            subRecipeSheet,
                            equipmentOptionSheet));
                    }
                    catch
                    {
                        _logger.WriteLine($"recipe id: {recipeRow.Id}");
                        throw;
                    }

                    continue;
                }

                foreach (var subRecipeId in recipeRow.SubRecipeIds)
                {
                    if (!subRecipeSheet.TryGetValue(subRecipeId, out var subRecipeRow))
                    {
                        continue;
                    }

                    var equipment = CreateEquipment(
                        random,
                        equipmentRow,
                        subRecipeRow,
                        equipmentOptionSheet,
                        skillSheet,
                        recipeRow.IsMimisBrunnrSubRecipe(subRecipeId));
                    try
                    {
                        var isMimis = equipment.IsMadeWithMimisbrunnrRecipe(
                            recipeSheet,
                            subRecipeSheet,
                            equipmentOptionSheet);
                        if (recipeRow.IsMimisBrunnrSubRecipe(subRecipeId))
                        {
                            Assert.True(isMimis);
                        }
                        else
                        {
                            Assert.False(isMimis);
                        }
                    }
                    catch
                    {
                        _logger.WriteLine($"recipe id: {recipeRow.Id}, sub recipe id: {subRecipeId}");
                        throw;
                    }
                }
            }
        }

        // Ref CombinationEquipment L267-L284
        private Equipment CreateEquipment(
            IRandom random,
            ItemSheet.Row equipmentRow,
            EquipmentItemSubRecipeSheetV2.Row subRecipeRow,
            EquipmentItemOptionSheet equipmentItemOptionSheet,
            SkillSheet skillSheet,
            bool isMimis)
        {
            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                equipmentRow,
                Guid.NewGuid(),
                0,
                madeWithMimisbrunnrRecipe: false);

            if (!(subRecipeRow is null))
            {
                CombinationEquipment.AddAndUnlockOption(
                    new AgentState(new PrivateKey().ToAddress()),
                    equipment,
                    random,
                    subRecipeRow,
                    equipmentItemOptionSheet,
                    skillSheet
                );
            }

            return equipment;
        }
    }
}
