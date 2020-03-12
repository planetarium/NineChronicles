using System;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using NUnit.Framework;
using UnityEngine;
using Inventory = Nekoyume.UI.Model.Inventory;

namespace Tests.EditMode
{
    public class InventoryTest
    {
        private TableSheets _tableSheets;

        [SetUp]
        public void SetUp()
        {
            _tableSheets = TableSheetsHelper.MakeTableSheets();
        }

        [TearDown]
        public void TearDown()
        {
            _tableSheets = null;
        }

        [Test]
        public void AddItem()
        {
            var inventory = new Inventory();
            
            var row = _tableSheets.EquipmentItemSheet.First;
            Assert.NotNull(row);

            var equipment = ItemFactory.CreateItemUsable(row, Guid.NewGuid(), -100);
            inventory.AddItem(equipment);
            Assert.AreEqual(1, inventory.Equipments.Count);
            
            equipment = ItemFactory.CreateItemUsable(row, Guid.NewGuid(), 100);
            inventory.AddItem(equipment);
            Assert.AreEqual(1, inventory.Equipments.Count);
            
            equipment = ItemFactory.CreateItemUsable(row, Guid.NewGuid(), -100);
            inventory.AddItem(equipment);
            Assert.AreEqual(2, inventory.Equipments.Count);
        }
    }
}
