using System;
using Nekoyume.Model.Item;
using NUnit.Framework;
using Tests.PlayMode.Fixtures;

namespace Tests.PlayMode
{
    public class ItemUsableTest : PlayModeTest
    {
        [Test]
        public void Equals()
        {
            var row = Nekoyume.Game.Game.instance.TableSheets.EquipmentItemSheet.First;
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
