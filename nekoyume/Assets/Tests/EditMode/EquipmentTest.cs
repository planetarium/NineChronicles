using System.Linq;
using Nekoyume;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class EquipmentTest
    {
        private TableSheets _tableSheets;

        [OneTimeSetUp]
        public void Init()
        {
            _tableSheets = new TableSheets();
            var request = Resources.Load<AddressableAssetsContainer>(Game.AddressableAssetsContainerPath);
            if (!(request is AddressableAssetsContainer addressableAssetsContainer))
                throw new FailedToLoadResourceException<AddressableAssetsContainer>(Game.AddressableAssetsContainerPath);

            var csvAssets = addressableAssetsContainer.tableCsvAssets;
            foreach (var asset in csvAssets)
            {
                _tableSheets.SetToSheet(asset.name, asset.text);
            }

            _tableSheets.ItemSheetInitialize();
            _tableSheets.QuestSheetInitialize();

        }

        [Test]
        public void LevelUp()
        {
            var recipeRow =
                _tableSheets.EquipmentItemRecipeSheet.Values.First(i => !i.SubRecipeIds.Any());
            var row = _tableSheets.EquipmentItemSheet.Values.First(i => i.Id == recipeRow.ResultEquipmentId);
            var equipment = (Equipment) ItemFactory.CreateItemUsable(row, default, default);
            var stat = equipment.StatsMap.GetStat(equipment.UniqueStatType);
            Assert.AreEqual(0, equipment.level);
            Assert.IsEmpty(equipment.GetOptions());
            equipment.LevelUp();
            Assert.AreEqual(1, equipment.level);
            Assert.AreEqual(decimal.ToInt32(stat + stat * 0.1m),
                equipment.StatsMap.GetStat(equipment.UniqueStatType));
            equipment.LevelUp();
            Assert.AreEqual(2, equipment.level);
            Assert.AreEqual(decimal.ToInt32(stat + stat * 0.2m),
                equipment.StatsMap.GetStat(equipment.UniqueStatType));
            equipment.LevelUp();
            Assert.AreEqual(3, equipment.level);
            Assert.AreEqual(decimal.ToInt32(stat + stat * 0.3m),
                equipment.StatsMap.GetStat(equipment.UniqueStatType));
        }
    }
}
