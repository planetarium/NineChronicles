using System;
using System.Collections;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI;
using NUnit.Framework;
using Tests.PlayMode.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class QuestPreparationTest : PlayModeTest
    {
        private QuestPreparation _widget;
        private Ring _ring;

        [UnitySetUp]
        public IEnumerator QuestPreparationSetup()
        {
            _widget = Widget.Find<QuestPreparation>();
            var data = Game.instance.TableSheets.EquipmentItemSheet.OrderedList.FirstOrDefault(row =>
                row.ItemSubType == ItemSubType.Ring);
            var ring = new Ring(data, Guid.NewGuid());
            _ring = ring;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator QuestPreparationTearDown()
        {
            foreach (var es in _widget.equipmentSlots)
            {
                es.Unequip();
            }

            yield return null;
        }

        [Test]
        public void FindRingSlotEmptyFirst()
        {
            var ringSlots =
                _widget.equipmentSlots.Where(es => es.itemSubType == ItemSubType.Ring && es.item?.Data is null);
            Assert.AreEqual(2, ringSlots.Count());

            var slot = _widget.FindSelectedItemSlot(ItemSubType.Ring);
            slot.Set(_ring);

            Assert.AreEqual(slot, _widget.equipmentSlots.First(es => es.itemSubType == ItemSubType.Ring));
        }

        [Test]
        public void FindRingSlotEmptySecond()
        {
            var ringSlots =
                _widget.equipmentSlots.Where(es => es.itemSubType == ItemSubType.Ring && es.item?.Data is null);
            Assert.AreEqual(2, ringSlots.Count());

            var slot = _widget.FindSelectedItemSlot(ItemSubType.Ring);
            slot.Set(_ring);
            var slot2 = _widget.FindSelectedItemSlot(ItemSubType.Ring);

            Assert.AreNotEqual(slot, slot2);
            Assert.AreEqual(_widget.equipmentSlots.Last(es => es.itemSubType == ItemSubType.Ring), slot2);
        }

        [Test]
        public void FindRingSlotFirst()
        {
            var ringSlots =
                _widget.equipmentSlots.Where(es => es.itemSubType == ItemSubType.Ring && es.item?.Data is null);
            Assert.AreEqual(2, ringSlots.Count());

            var slot = _widget.FindSelectedItemSlot(ItemSubType.Ring);
            slot.Set(_ring);
            var slot2 = _widget.FindSelectedItemSlot(ItemSubType.Ring);
            slot2.Set(_ring);
            var slot3 = _widget.FindSelectedItemSlot(ItemSubType.Ring);

            Assert.AreNotEqual(slot, slot2);
            Assert.AreEqual(slot, slot3);
            Assert.AreNotEqual(slot2, slot3);
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator HackAndSlash()
        {
            miner = new MinerFixture("hack_and_slash");

            // CreateAvatar
            Widget.Find<Title>().OnClick();
            Widget.Find<Synopsis>().End();
            yield return new WaitUntil(() => Widget.Find<Login>().ready);
            Widget.Find<Login>().SlotClick(2);
            var loginDetail = Widget.Find<LoginDetail>();
            loginDetail.CreateAndLogin("HackAndSlash");
            yield return new WaitUntil(() => Game.instance.Agent.StagedTransactions.Any());
            var createAvatarTx = Game.instance.Agent.StagedTransactions.First();
            yield return miner.CoMine(createAvatarTx);
            yield return new WaitWhile(() => States.Instance.CurrentAvatarState is null);
            yield return new WaitUntil(() => Widget.Find<Login>().ready);

            // Login
            Widget.Find<Login>().SlotClick(2);
            Widget.Find<LoginDetail>().LoginClick();
            yield return new WaitUntil(() => GameObject.Find("room"));

            var dialog = Widget.Find<Dialog>();
            yield return new WaitUntil(() => dialog.isActiveAndEnabled);
            while (dialog.isActiveAndEnabled)
            {
                dialog.Skip();
                yield return null;
            }

            _widget.Show();
            _widget.QuestClick(false);
            yield return new WaitUntil(() => Game.instance.Agent.StagedTransactions.Any());
            // Transaction.Id 가 랜덤하게 생성되어 순서가 보장이 되지 않기때문에 정렬처리
            var tx = Game.instance.Agent.StagedTransactions.First();
            yield return miner.CoMine(tx);
            yield return new WaitUntil(() => Widget.Find<BattleResult>().isActiveAndEnabled);
            Widget.Find<BattleResult>().GoToMain();
        }
    }
}
