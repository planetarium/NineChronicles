using System.Linq;
using Nekoyume.Data;
using Nekoyume.Game.Item;
using Nekoyume.UI;
using NUnit.Framework;

namespace Tests
{
    public class QuestPreparationTest
    {
        private readonly QuestPreparation _widget;
        private readonly Ring _ring;

        public QuestPreparationTest()
        {
            _widget = Widget.Find<QuestPreparation>();
            var data = Tables.instance.ItemEquipment.Values.First();
            data.cls = "Ring";
            var ring = new Ring(data);
            _ring = ring;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var es in _widget.equipmentSlots)
            {
                es.Unequip();
            }
        }

        [Test]
        public void FindRingSlotEmptyFirst()
        {
            var ringSlots =
                _widget.equipmentSlots.Where(es => es.type == ItemBase.ItemType.Ring && es.item?.Data is null);
            Assert.AreEqual(2, ringSlots.Count());

            var slot = _widget.FindSelectedItemSlot(ItemBase.ItemType.Ring);
            slot.Set(_ring);

            Assert.AreEqual(slot, _widget.equipmentSlots.First(es => es.type == ItemBase.ItemType.Ring));
        }

        [Test]
        public void FindRingSlotEmptySecond()
        {
            var ringSlots = _widget.equipmentSlots.Where(es => es.type == ItemBase.ItemType.Ring && es.item?.Data is null);
            Assert.AreEqual(2, ringSlots.Count());

            var slot = _widget.FindSelectedItemSlot(ItemBase.ItemType.Ring);
            slot.Set(_ring);
            var slot2 = _widget.FindSelectedItemSlot(ItemBase.ItemType.Ring);

            Assert.AreNotEqual(slot, slot2);
            Assert.AreEqual(_widget.equipmentSlots.Last(es => es.type == ItemBase.ItemType.Ring), slot2);
        }

        [Test]
        public void FindRingSlotFirst()
        {
            var ringSlots =
                _widget.equipmentSlots.Where(es => es.type == ItemBase.ItemType.Ring && es.item?.Data is null);
            Assert.AreEqual(2, ringSlots.Count());

            var slot = _widget.FindSelectedItemSlot(ItemBase.ItemType.Ring);
            slot.Set(_ring);
            var slot2 = _widget.FindSelectedItemSlot(ItemBase.ItemType.Ring);
            slot2.Set(_ring);
            var slot3 = _widget.FindSelectedItemSlot(ItemBase.ItemType.Ring);

            Assert.AreNotEqual(slot, slot2);
            Assert.AreEqual(slot, slot3);
            Assert.AreNotEqual(slot2, slot3);
        }

    }
}
