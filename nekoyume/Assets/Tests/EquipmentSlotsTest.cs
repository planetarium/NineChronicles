using System;
using System.Collections;
using Nekoyume.Game.Item;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class EquipmentSlotsTest
    {
        private EquipmentSlotsFixture _fx;

        [UnityTest]
        public IEnumerator TryGetReturnFalse()
        {
            var fixture = new MonoBehaviourTest<EquipmentSlotsFixture>();
            yield return fixture;
            _fx = fixture.component;
            yield return new WaitUntil(()=> _fx.IsTestFinished);

            var es = _fx.GetComponent<EquipmentSlots>();
            Assert.IsFalse(es.TryGet(ItemBase.ItemType.Weapon, out var slot));
            Assert.Null(slot);
        }

        [UnityTest]
        public IEnumerator TryGetReturnTrue()
        {
            var fixture = new MonoBehaviourTest<EquipmentSlotsFixture>();
            yield return fixture;
            _fx = fixture.component;
            yield return new WaitUntil(()=> _fx.IsTestFinished);

            var es = _fx.GetComponent<EquipmentSlots>();

            es.slots = new[] {_fx.GetComponent<EquipSlot>()};

            Assert.IsTrue(_fx.GetComponent<EquipmentSlots>().TryGet(ItemBase.ItemType.Armor, out var slot));
            Assert.AreEqual(ItemBase.ItemType.Armor, slot.type);
            Assert.NotNull(slot);
        }


        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.Destroy(_fx);
        }
    }

    public class EquipmentSlotsFixture : MonoBehaviour, IMonoBehaviourTest
    {
        public bool IsTestFinished { get; set; }

        public void Start()
        {
            LogAssert.Expect(LogType.Exception,
                "NotFoundComponentException`1: Not found `EquipSlot` component.");
            var slot = gameObject.AddComponent<EquipSlot>();
            slot.type = ItemBase.ItemType.Armor;
            gameObject.AddComponent<EquipmentSlots>().slots = new [] {slot};
            IsTestFinished = true;
        }
    }
}
