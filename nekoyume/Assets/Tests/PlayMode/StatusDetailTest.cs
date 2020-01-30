using System;
using System.Linq;
using Libplanet;
using Nekoyume.Data;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Factory;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI;
using NUnit.Framework;

namespace Tests.PlayMode
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
            var avatarState = new AvatarState(address, agentAddress, 1, Game.instance.TableSheets);
            var go = PlayerFactory.Create(avatarState);
            _player = go.GetComponent<Player>();

            var data = Game.instance.TableSheets.EquipmentItemSheet.OrderedList.FirstOrDefault(row =>
                row.ItemSubType == ItemSubType.Ring);
            _ring = new Ring(data, Guid.NewGuid());
            _player.Inventory.AddItem(_ring);
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
            Assert.IsEmpty(_player.Equipments.Where(i => i is Ring));
            _ring.Equip();
            Assert.AreEqual(1, _player.Equipments.Count(i => i is Ring));
            _widget.Show();
            var ringSlots = _widget.equipmentSlots.Where(i => i.itemSubType == ItemSubType.Ring).ToList();
            Assert.AreEqual(2, ringSlots.Count);
            Assert.AreEqual(1, ringSlots.Count(i => i.item is Ring));
        }

        [Test]
        public void ShowWithRings()
        {
            Assert.IsEmpty(_player.Equipments.Where(i => i is Ring));
            _ring.Equip();
            var ring = new Ring(_ring.Data, Guid.NewGuid());
            ring.Equip();
            _player.Inventory.AddItem(ring);
            Assert.AreEqual(2, _player.Equipments.Count(i => i is Ring));
            _widget.Show();
            var ringSlots = _widget.equipmentSlots.Where(i => i.itemSubType == ItemSubType.Ring).ToList();
            Assert.AreEqual(2, ringSlots.Count);
            Assert.AreEqual(2, ringSlots.Count(i => i.item is Ring));
        }

    }
}
