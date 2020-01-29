using System;
using Nekoyume.Game.Factory;
using Nekoyume.Model.Item;
using NUnit.Framework;
using Tests.PlayMode.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class InventoryTest : PlayModeTest
    {
        [Test]
        public void TryGetAddedItemFromTrue()
        {
            var inventory = new Inventory();
            var updatedInventory = new Inventory();
            var row = Nekoyume.Game.Game.instance.TableSheets.EquipmentItemSheet.First;
            var itemUsable = (ItemUsable) ItemFactory.Create(row, new Guid());
            updatedInventory.AddItem(itemUsable);
            Assert.IsTrue(updatedInventory.TryGetAddedItemFrom(inventory, out var result1));
            Assert.AreEqual(itemUsable, result1);
        }

        [Test]
        public void TryGetAddedItemFromFalse()
        {
            var inventory = new Inventory();
            var updatedInventory = new Inventory();
            var row = Nekoyume.Game.Game.instance.TableSheets.EquipmentItemSheet.First;
            var itemUsable = (ItemUsable) ItemFactory.Create(row, new Guid());
            inventory.AddItem(itemUsable);
            updatedInventory.AddItem(itemUsable);
            updatedInventory.AddItem(itemUsable);
            Assert.IsFalse(updatedInventory.TryGetAddedItemFrom(inventory, out var result2));
            Assert.IsNull(result2);

        }

        [Test]
        public void TryGetAddedItemFromInvalidCastException()
        {
            var inventory = new Inventory();
            var updatedInventory = new Inventory();
            var row = Nekoyume.Game.Game.instance.TableSheets.MaterialItemSheet.First;
            var item = (Nekoyume.Model.Item.Material) ItemFactory.Create(row, new Guid());
            LogAssert.Expect(LogType.Error, "Item Material: 100000 is not ItemUsable.");
            updatedInventory.AddItem(item);
            Assert.IsFalse(updatedInventory.TryGetAddedItemFrom(inventory, out var result2));
            Assert.IsNull(result2);
        }

        [Test]
        public void Item()
        {
            var row = Nekoyume.Game.Game.instance.TableSheets.EquipmentItemSheet.First;
            var itemUsable1 = (ItemUsable) ItemFactory.Create(row, new Guid());

            var item1 = new Inventory.Item(itemUsable1);
            var item2 = new Inventory.Item(itemUsable1);
            Assert.AreEqual(item1, item2);
        }
    }
}
