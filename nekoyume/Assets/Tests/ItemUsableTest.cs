using System;
using System.Linq;
using Nekoyume.Data;
using Nekoyume.Game.Factory;
using NUnit.Framework;

namespace Tests
{
    public class ItemUsableTest : PlayModeTest
    {
        [Test]
        public void Equals()
        {
            var row = Tables.instance.ItemEquipment.Values.First();
            var id1 = new Guid();
            var item1 = ItemFactory.Create(row, Guid.NewGuid());
            var item2 = ItemFactory.Create(row, Guid.NewGuid());
            var item3 = ItemFactory.Create(row, id1);
            var item4 = ItemFactory.Create(row, id1);
            Assert.AreNotEqual(item2, item1);
            Assert.AreEqual(item4, item3);
        }
    }
}
