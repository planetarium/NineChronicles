using System;
using System.Linq;
using Nekoyume.Data;
using Nekoyume.Game.Item;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class InventoryTest
    {
        [Test]
        public void TryGetAddedItemFromTrue()
        {
            var inventory = new Inventory();
            var updatedInventory = new Inventory();
            var row = Tables.instance.ItemEquipment.Values.First();
            var itemUsable = (ItemUsable) ItemBase.ItemFactory(row, new Guid());
            updatedInventory.AddNonFungibleItem(itemUsable);
            Assert.IsTrue(updatedInventory.TryGetAddedItemFrom(inventory, out var result1));
            Assert.AreEqual(itemUsable, result1);
        }

        [Test]
        public void TryGetAddedItemFromFalse()
        {
            var inventory = new Inventory();
            var updatedInventory = new Inventory();
            var row = Tables.instance.ItemEquipment.Values.First();
            var itemUsable = (ItemUsable) ItemBase.ItemFactory(row, new Guid());
            inventory.AddNonFungibleItem(itemUsable);
            updatedInventory.AddNonFungibleItem(itemUsable);
            updatedInventory.AddNonFungibleItem(itemUsable);
            Assert.IsFalse(updatedInventory.TryGetAddedItemFrom(inventory, out var result2));
            Assert.IsNull(result2);

        }

        [Test]
        public void TryGetAddedItemFromInvalidCastException()
        {
            var inventory = new Inventory();
            var updatedInventory = new Inventory();
            var row = Tables.instance.Item.Values.First();
            var item = (Nekoyume.Game.Item.Material) ItemBase.ItemFactory(row, new Guid());
            LogAssert.Expect(LogType.Error, "Item Material: 100000 is not ItemUsable.");
            updatedInventory.AddFungibleItem(item);
            Assert.IsFalse(updatedInventory.TryGetAddedItemFrom(inventory, out var result2));
            Assert.IsNull(result2);
        }

        [Test]
        public void Item()
        {
            var row = Tables.instance.ItemEquipment.Values.First();
            var itemUsable1 = (ItemUsable) ItemBase.ItemFactory(row, new Guid());

            var item1 = new Inventory.Item(itemUsable1);
            var item2 = new Inventory.Item(itemUsable1);
            Assert.AreEqual(item1, item2);
        }
    }
}
