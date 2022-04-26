namespace Lib9c.Tests
{
    using System.Collections.Generic;
    using Nekoyume.Helper;
    using Nekoyume.TableData;
    using Xunit;

    public class CrystalCalculatorTest
    {
        private readonly EquipmentItemRecipeSheet _equipmentItemRecipeSheet;
        private readonly WorldUnlockSheet _worldUnlockSheet;

        public CrystalCalculatorTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _equipmentItemRecipeSheet = tableSheets.EquipmentItemRecipeSheet;
            _worldUnlockSheet = tableSheets.WorldUnlockSheet;
        }

        [Theory]
        [InlineData(new[] { 2 }, 100)]
        [InlineData(new[] { 2, 3 }, 200)]
        public void CalculateRecipeUnlockCost(IEnumerable<int> recipeIds, int expected)
        {
            Assert.Equal(expected * CrystalCalculator.CRYSTAL, CrystalCalculator.CalculateRecipeUnlockCost(recipeIds, _equipmentItemRecipeSheet));
        }

        [Theory]
        [InlineData(new[] { 2 }, 500)]
        [InlineData(new[] { 2, 3 }, 1000)]
        public void CalculateWorldUnlockCost(IEnumerable<int> worldIds, int expected)
        {
            Assert.Equal(expected * CrystalCalculator.CRYSTAL, CrystalCalculator.CalculateWorldUnlockCost(worldIds, _worldUnlockSheet));
        }
    }
}
