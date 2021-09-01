using System.Collections.Generic;
using System.Linq;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class CombinationEquipmentTest
    {
        private TableSheets _tableSheets;

        [OneTimeSetUp]
        public void Init()
        {
            _tableSheets = TableSheetsHelper.MakeTableSheets();
        }

        [Test]
        public void SelectOptionEmptyByLimit()
        {
            var row = _tableSheets.EquipmentItemSubRecipeSheet.Values.First();
            row.Set(new List<string>
            {
                "1", "3", "1", "1", "1", "306040", "3", "306023", "2", "306024", "1", "1", "0.55", "4", "0.5", "2", "0.05",
                "", "", "0"
            });
            var equipment = (Equipment) ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.Values.First(),
                default,
                default
            );
            var optionIds = CombinationEquipment4.SelectOption(
                _tableSheets.EquipmentItemOptionSheet,
                _tableSheets.SkillSheet,
                row,
                new Cheat.DebugRandom(),
                equipment
            );
            Assert.IsEmpty(optionIds);
            Assert.IsEmpty(equipment.GetOptions());
        }

        [Test]
        public void SelectOptionEmptyByRatio()
        {
            var row = _tableSheets.EquipmentItemSubRecipeSheet.Values.First();
            row.Set(new List<string>
            {
                "1", "3", "1", "1", "1", "306040", "3", "306023", "2", "306024", "1", "1", "0", "4", "0", "2", "0",
                "", "", "2"
            });
            var equipment = (Equipment) ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.Values.First(),
                default,
                default
            );
            var optionIds = CombinationEquipment4.SelectOption(
                _tableSheets.EquipmentItemOptionSheet,
                _tableSheets.SkillSheet,
                row,
                new Cheat.DebugRandom(),
                equipment
            );
            Assert.IsEmpty(optionIds);
            Assert.IsEmpty(equipment.GetOptions());
        }

        [Test]
        public void SelectOption([Values(1, 2)] int expected)
        {
            var row = _tableSheets.EquipmentItemSubRecipeSheet.Values.First();
            // ATK, Skill, SPD 옵션 3종류
            row.Set(new List<string>
            {
                "1", "3", "1", "1", "1", "306040", "3", "306023", "2", "306024", "1", "1", "0.5", "7", "0.3", "17", "0.2",
                "", "", expected.ToString()
            });
            var equipment = (Equipment) ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.Values.First(),
                default,
                default
            );
            var optionIds = CombinationEquipment4.SelectOption(
                _tableSheets.EquipmentItemOptionSheet,
                _tableSheets.SkillSheet,
                row,
                new Cheat.DebugRandom(),
                equipment
            );
            Assert.IsNotEmpty(optionIds);
            Assert.AreEqual(expected, optionIds.Count);
            Assert.AreEqual(expected, equipment.GetOptionCount());
        }
    }
}
