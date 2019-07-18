using System.Linq;
using Nekoyume.Data;
using Nekoyume.Game.Item;
using NUnit.Framework;

namespace Tests
{
    public class InventoryTest
    {
        [Test]
        public void TryGetAddedItemFrom()
        {
            var inventory = new Inventory();
            var id = Tables.instance.ItemEquipment.Values.First().id;
            var inventory2 = new Inventory();
            inventory.AddNonFungibleItem(id);
            inventory2.AddNonFungibleItem(id);
            Assert.IsTrue(inventory2.TryGetAddedItemFrom(inventory, out _));
            Assert.IsFalse(inventory2.TryGetAddedItemFrom(inventory2, out _));
        }
    }
}
