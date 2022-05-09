namespace Lib9c.Tests
{
    using System.Collections;
    using System.Collections.Generic;
    using Nekoyume.Helper;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class CrystalCalculatorTest
    {
        private readonly TableSheets _tableSheets;
        private readonly EquipmentItemRecipeSheet _equipmentItemRecipeSheet;
        private readonly WorldUnlockSheet _worldUnlockSheet;

        public CrystalCalculatorTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _equipmentItemRecipeSheet = _tableSheets.EquipmentItemRecipeSheet;
            _worldUnlockSheet = _tableSheets.WorldUnlockSheet;
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
        public void CalculateCrystal((int equipmentId, int level)[] equipmentInfos, int monsterCollectionLevel, bool enhancementFaield, int expected)
        {
            var equipmentList = new List<Equipment>();
            foreach (var (equipmentId, level) in equipmentInfos)
            {
                var row = _tableSheets.EquipmentItemSheet[equipmentId];
                var equipment =
                    ItemFactory.CreateItemUsable(row, default, 0, level);
                equipmentList.Add((Equipment)equipment);
            }

            Assert.Equal(
                expected * CrystalCalculator.CRYSTAL,
                CrystalCalculator.CalculateCrystal(
                    equipmentList,
                    _tableSheets.CrystalEquipmentGrindingSheet,
                    monsterCollectionLevel,
                    _tableSheets.CrystalMonsterCollectionMultiplierSheet,
                    enhancementFaield
                )
            );
        }

        private class CalculateCrystalData : IEnumerable<object[]>
        {
            private readonly List<object[]> _data = new List<object[]>
            {
                new object[]
                {
                    new[]
                    {
                        (10100000, 0),
                        (10100000, 2),
                    },
                    0,
                    false,
                    300,
                },
                new object[]
                {
                    new[]
                    {
                        (10100000, 0),
                    },
                    0,
                    true,
                    50,
                },
                new object[]
                {
                    new[]
                    {
                        (10100000, 3),
                    },
                    3,
                    true,
                    260,
                },
                new object[]
                {
                    new[]
                    {
                        (10100000, 1),
                        (10100000, 2),
                    },
                    3,
                    false,
                    390,
                },
            };

            public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();
        }
    }
}
