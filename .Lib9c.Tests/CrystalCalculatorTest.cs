namespace Lib9c.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Libplanet.Assets;
    using Nekoyume.Helper;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Crystal;
    using Xunit;

    public class CrystalCalculatorTest
    {
        private readonly TableSheets _tableSheets;
        private readonly EquipmentItemRecipeSheet _equipmentItemRecipeSheet;
        private readonly WorldUnlockSheet _worldUnlockSheet;
        private readonly CrystalMaterialCostSheet _crystalMaterialCostSheet;
        private readonly Currency _ncgCurrency;

        public CrystalCalculatorTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _equipmentItemRecipeSheet = _tableSheets.EquipmentItemRecipeSheet;
            _worldUnlockSheet = _tableSheets.WorldUnlockSheet;
            _crystalMaterialCostSheet = _tableSheets.CrystalMaterialCostSheet;
            _ncgCurrency = new Currency("NCG", 2, minters: null);
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

        [Theory]
        [ClassData(typeof(CalculateCrystalData))]
        public void CalculateCrystal((int EquipmentId, int Level)[] equipmentInfos, int stakedAmount, bool enhancementFailed, int expected)
        {
            var equipmentList = new List<Equipment>();
            foreach (var (equipmentId, level) in equipmentInfos)
            {
                var row = _tableSheets.EquipmentItemSheet[equipmentId];
                var equipment =
                    ItemFactory.CreateItemUsable(row, default, 0, level);
                equipmentList.Add((Equipment)equipment);
            }

            var actual = CrystalCalculator.CalculateCrystal(
                default,
                equipmentList,
                stakedAmount * _ncgCurrency,
                enhancementFailed,
                _tableSheets.CrystalEquipmentGrindingSheet,
                _tableSheets.CrystalMonsterCollectionMultiplierSheet,
                _tableSheets.StakeRegularRewardSheet
            );

            Assert.Equal(
                expected * CrystalCalculator.CRYSTAL,
                actual);
        }

        [Theory]
        [InlineData(2, 1, 200)]
        [InlineData(1, 2, 50)]
        public void CalculateCombinationCost(int psCount, int bpsCount, int expected)
        {
            var crystal = 100 * CrystalCalculator.CRYSTAL;
            var ps = new CrystalCostState(default, crystal * psCount);
            var bps = new CrystalCostState(default, crystal * bpsCount);

            Assert.Equal(expected * CrystalCalculator.CRYSTAL, CrystalCalculator.CalculateCombinationCost(crystal, ps, bps));
        }

        [Fact]
        public void CalculateRandomBuffCost()
        {
            var stageBuffGachaSheet = _tableSheets.CrystalStageBuffGachaSheet;
            foreach (var row in stageBuffGachaSheet.Values)
            {
                var expectedCost = row.CRYSTAL * CrystalCalculator.CRYSTAL;
                Assert.Equal(
                    expectedCost,
                    CrystalCalculator.CalculateBuffGachaCost(row.StageId, 5, stageBuffGachaSheet));
                Assert.Equal(
                    expectedCost * 3,
                    CrystalCalculator.CalculateBuffGachaCost(row.StageId, 10, stageBuffGachaSheet));
            }
        }

        [Theory]
        [InlineData(302000, 1, 100, null)]
        [InlineData(302003, 2, 200, null)]
        [InlineData(306068, 1, 100, typeof(ArgumentException))]
        public void CalculateMaterialCost(int materialId, int materialCount, int expected, Type exc)
        {
            if (_crystalMaterialCostSheet.ContainsKey(materialId))
            {
                var cost = CrystalCalculator.CalculateMaterialCost(materialId, materialCount, _crystalMaterialCostSheet);
                Assert.Equal(expected * CrystalCalculator.CRYSTAL, cost);
            }
            else
            {
                Assert.Throws(exc, () => CrystalCalculator.CalculateMaterialCost(materialId, materialCount, _crystalMaterialCostSheet));
            }
        }

        private class CalculateCrystalData : IEnumerable<object[]>
        {
            private readonly List<object[]> _data = new List<object[]>
            {
                // 100 + (2^0 - 1) * 100 = 100
                // enchant level 2
                // 200 + (2^2 - 1) * 100 = 500
                // total 600
                new object[]
                {
                    new[]
                    {
                        (10100000, 0),
                        (10110000, 2),
                    },
                    10,
                    false,
                    600,
                },
                new object[]
                {
                    // enchant failed
                    // 100 + (2^0 -1) * 100 % 2 = 50
                    // total 50
                    new[]
                    {
                        (10100000, 0),
                    },
                    10,
                    true,
                    50,
                },
                // enchant level 3 & failed
                // (200 + (2^3 - 1) * 100) % 2 = 450
                // multiply by staking
                // 450 * 0.1 = 45
                // total 495
                new object[]
                {
                    new[]
                    {
                        (10110000, 3),
                    },
                    100,
                    true,
                    495,
                },
                // enchant level 1
                // 100 + (2^1 - 1) * 100 = 200
                // enchant level 2
                // 200 + (2^2 - 1) * 100 = 500
                // multiply by staking
                // 700 * 0.1 = 70
                // total 770
                new object[]
                {
                    new[]
                    {
                        (10100000, 1),
                        (10110000, 2),
                    },
                    100,
                    false,
                    770,
                },
                // enchant level 1
                // 200 + (2^1 - 1) * 100 = 300
                new object[]
                {
                    new[]
                    {
                        (10110000, 1),
                    },
                    0,
                    false,
                    300,
                },
            };

            public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();
        }
    }
}
