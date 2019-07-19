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
            var item1 = ItemBase.ItemFactory(row);
            var item2 = ItemBase.ItemFactory(row);
            var item3 = ItemBase.ItemFactory(row, id: "0");
            var item4 = ItemBase.ItemFactory(row, id: "0");
            Assert.AreNotEqual(item2, item1);
            Assert.AreEqual(item4, item3);
        }
    }
}
