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
                    0));
        }

        [Fact]
        public void CalculateEnhancementCost_Throw_ArgumentException()
        {
            var row = _petCostSheet.First!;
            var petId = row.PetId;
            Assert.Throws<ArgumentException>(() =>
                PetHelper.CalculateEnhancementCost(
                    _petCostSheet,
                    -1,
                    0,
                    1));
            Assert.Throws<ArgumentException>(() =>
                PetHelper.CalculateEnhancementCost(
                    _petCostSheet,
                    petId,
                    -1,
                    1));
            Assert.Throws<ArgumentException>(() =>
                PetHelper.CalculateEnhancementCost(
                    _petCostSheet,
                    petId,
                    0,
                    -1));

            var cost = row.Cost.LastOrDefault(e =>
                e.NcgQuantity > 0 ||
                e.SoulStoneQuantity > 0);
            Assert.NotNull(cost);
            Assert.Throws<ArgumentException>(() =>
                PetHelper.CalculateEnhancementCost(
                    _petCostSheet,
                    petId,
                    cost.Level,
                    cost.Level + 1));
        }

        [Fact]
        public void CalculateEnhancementCost_Throw_ArgumentOutOfRangeException()
        {
            var row = _petCostSheet.First!;
            var petId = row.PetId;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PetHelper.CalculateEnhancementCost(
                    _petCostSheet,
                    petId,
                    2,
                    1));

            var cost = row.Cost.FirstOrDefault(e =>
                e.NcgQuantity > 0 ||
                e.SoulStoneQuantity > 0);
            Assert.NotNull(cost);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PetHelper.CalculateEnhancementCost(
                    _petCostSheet,
                    petId,
                    cost.Level + 1,
                    cost.Level));
        }
    }
}
