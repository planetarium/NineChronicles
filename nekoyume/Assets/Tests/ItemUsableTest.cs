using System;
using System.Linq;
using Nekoyume.Data;
using Nekoyume.Game.Item;
using NUnit.Framework;

namespace Tests
{
    public class ItemUsableTest
    {
        [Test]
        public void Equals()
        {
            var row = Tables.instance.ItemEquipment.Values.First();
            var id1 = new Guid();
            var item1 = ItemBase.ItemFactory(row, Guid.NewGuid());
            var item2 = ItemBase.ItemFactory(row, Guid.NewGuid());
            var item3 = ItemBase.ItemFactory(row, id1);
            var item4 = ItemBase.ItemFactory(row, id1);
            Assert.AreNotEqual(item2, item1);
            Assert.AreEqual(item4, item3);
        }
    }
}
