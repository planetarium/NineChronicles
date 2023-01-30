namespace Lib9c.Tests.Action
{
    using System;
    using System.Linq;
    using Nekoyume.Helper;
    using Xunit;

    public class PetHelperTest
    {
        [Fact]
        public void CalculateEnhancementCost()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            var petCostSheet = new TableSheets(sheets).PetCostSheet;
            var firstPetCostRow = petCostSheet.First;
            if (firstPetCostRow is null)
            {
                throw new Exception();
            }

            var firstLevel = firstPetCostRow.Cost.First().Level;
            var lastLevel = firstPetCostRow.Cost.Last().Level;
            var (ncgCost, soulStoneCost) = (0, 0);
            for (var i = firstLevel; i <= lastLevel; i++)
            {
                firstPetCostRow.TryGetCost(i, out var cost);
                ncgCost += cost.NcgQuantity;
                soulStoneCost += cost.SoulStoneQuantity;
            }

            var actualCost = PetHelper.CalculateEnhancementCost(
                petCostSheet,
                firstPetCostRow.PetId,
                firstLevel - 1,
                lastLevel);
            Assert.Equal(ncgCost, actualCost.ncgQuantity);
            Assert.Equal(soulStoneCost, actualCost.soulStoneQuantity);
        }
    }
}
