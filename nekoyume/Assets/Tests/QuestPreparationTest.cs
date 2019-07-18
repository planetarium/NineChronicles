using System.Collections;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Data;
using Nekoyume.Game.Item;
using Nekoyume.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class QuestPreparationTest
    {
        private readonly QuestPreparation _widget;
        private readonly Ring _ring;
        private MinerFixture _miner;

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

            _miner?.TearDown();
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

        [UnityTest]
        public IEnumerator HackAndSlash()
        {
            _miner = new MinerFixture("hack_and_slash");

            // CreateAvatar
            Widget.Find<Title>().OnClick();
            Widget.Find<Synopsis>().End();
            yield return new WaitUntil(() => Widget.Find<Login>().ready);
            Widget.Find<Login>().SlotClick(2);
            Widget.Find<LoginDetail>().CreateClick();
            yield return new WaitUntil(() => AgentController.Agent.Transactions.Any());
            var createAvatarTx = AgentController.Agent.Transactions.First().Value;
            yield return _miner.CoMine(createAvatarTx);
            yield return new WaitWhile(() => States.Instance.currentAvatarState.Value is null);
            yield return new WaitUntil(() => Widget.Find<Login>().ready);

            // Login
            Widget.Find<Login>().SlotClick(2);
            Widget.Find<LoginDetail>().LoginClick();
            yield return new WaitUntil(() => GameObject.Find("room"));

            Assert.IsNull(States.Instance.currentAvatarState.Value.battleLog);
            _widget.Show();
            var current = AgentController.Agent.Transactions.Count;
            _widget.QuestClick(false);
            yield return new WaitUntil(() => AgentController.Agent.Transactions.Count > current);
            // Transaction.Id 가 랜덤하게 생성되어 순서가 보장이 되지 않기때문에 정렬처리
            var tx = AgentController.Agent.Transactions.Values.OrderByDescending(t => t.Timestamp).First();
            yield return _miner.CoMine(tx);
            yield return new WaitWhile(() => _widget.gameObject.activeSelf);
            Assert.IsNotNull(States.Instance.currentAvatarState.Value.battleLog);
        }

    }
}
