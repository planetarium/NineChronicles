using System.Linq;
using Libplanet;
using Nekoyume.Data;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Item;
using Nekoyume.State;
using Nekoyume.UI;
using NUnit.Framework;

namespace Tests
{
    public class StatusDetailTest
    {
        private readonly Player _player;
        private readonly Ring _ring;
        private readonly StatusDetail _widget;

        public StatusDetailTest()
        {
            var address = new Address();
            var agentAddress = new Address();
            var avatarState = new AvatarState(address, agentAddress);
            var go = Game.instance.stage.playerFactory.Create(avatarState);
            _player = go.GetComponent<Player>();

            var data = Tables.instance.ItemEquipment.Values.First();
            data.cls = "Ring";
            _ring = new Ring(data);
            _player.Inventory.AddNonFungibleItem(_ring);
            _widget = Widget.Find<StatusDetail>();

        }

        [TearDown]
        public void TearDown()
        {
            _ring.Unequip();
        }

        [Test]
        public void ShowWithRing()
        {
            Assert.IsEmpty(_player.equipments.Where(i => i is Ring));
            _ring.Equip();
            Assert.AreEqual(1, _player.equipments.Count(i => i is Ring));
            _widget.Show();
            var ringSlots = _widget.equipmentSlots.Where(i => i.type == ItemBase.ItemType.Ring).ToList();
            Assert.AreEqual(2, ringSlots.Count);
            Assert.AreEqual(1, ringSlots.Count(i => i.item is Ring));
        }

        [Test]
        public void ShowWithRings()
        {
            Assert.IsEmpty(_player.equipments.Where(i => i is Ring));
            _ring.Equip();
            var ring = new Ring(_ring.Data);
            ring.Equip();
            _player.Inventory.AddNonFungibleItem(ring);
            Assert.AreEqual(2, _player.equipments.Count(i => i is Ring));
            _widget.Show();
            var ringSlots = _widget.equipmentSlots.Where(i => i.type == ItemBase.ItemType.Ring).ToList();
            Assert.AreEqual(2, ringSlots.Count);
            Assert.AreEqual(2, ringSlots.Count(i => i.item is Ring));
        }

    }
}
