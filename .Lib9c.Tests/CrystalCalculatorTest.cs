namespace Lib9c.Tests
{
    using System.Collections.Generic;
    using Nekoyume.Helper;
    using Nekoyume.TableData;
    using Xunit;

    public class CrystalCalculatorTest
    {
        private readonly EquipmentItemRecipeSheet _sheet;

        public CrystalCalculatorTest()
        {
            _sheet = new TableSheets(TableSheetsImporter.ImportSheets()).EquipmentItemRecipeSheet;
        }

        [Theory]
        [InlineData(new[] { 2 }, 100)]
        [InlineData(new[] { 2, 3 }, 200)]
        public void CalculateCost(IEnumerable<int> recipeIds, int expected)
        {
            Assert.Equal(expected * CrystalCalculator.CRYSTAL, CrystalCalculator.CalculateCost(recipeIds, _sheet));
        }
    }
}
