namespace Lib9c.Tests.Helper
{
    using System;
    using System.Linq;
    using Nekoyume.Helper;
    using Nekoyume.TableData.Pet;
    using Xunit;

    public class PetHelperTest
    {
        private readonly PetCostSheet _petCostSheet;

        public PetHelperTest()
        {
            Assert.True(TableSheetsImporter.TryGetCsv(nameof(PetCostSheet), out var csv));
            _petCostSheet = new PetCostSheet();
            _petCostSheet.Set(csv);
        }

        [Fact]
        public void CalculateEnhancementCost()
        {
            var row = _petCostSheet.First!;
            var petId = row.PetId;
            var cost = row.Cost.FirstOrDefault(cost =>
                cost.NcgQuantity > 0 ||
                cost.SoulStoneQuantity > 0);
            Assert.NotNull(cost);
            PetHelper.CalculateEnhancementCost(
                _petCostSheet,
                petId,
                0,
                cost.Level);
        }

        [Fact]
        public void CalculateEnhancementCost_Throw_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                PetHelper.CalculateEnhancementCost(
                    null,
                    0,
                    0,
                    1));
        }

        [Theory]
        [InlineData(-1, 0, 0)]
        [InlineData(null, 0, 0)]
        [InlineData(null, -1, 0)]
        [InlineData(null, 0, -1)]
        public void CalculateEnhancementCost_Throw_ArgumentException_PetId_PetLevel(
            int? petId,
            int currentLevel,
            int targetLevel)
        {
            petId ??= _petCostSheet.First!.PetId;
            Assert.Throws<ArgumentException>(() =>
                PetHelper.CalculateEnhancementCost(
                    _petCostSheet,
                    petId.Value,
                    currentLevel,
                    targetLevel));
        }

        [Fact]
        public void CalculateEnhancementCost_Throw_ArgumentException_Undefined_PetLevel()
        {
            var row = _petCostSheet.First!;
            var cost = row.Cost.LastOrDefault(e =>
                e.NcgQuantity > 0 ||
                e.SoulStoneQuantity > 0);
            Assert.NotNull(cost);
            Assert.Throws<ArgumentException>(() =>
                PetHelper.CalculateEnhancementCost(
                    _petCostSheet,
                    row.PetId,
                    cost.Level,
                    cost.Level + 1));
        }
    }
}
