using System.Collections;
using Nekoyume.Model.Item;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Tests.PlayMode
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
            Assert.IsFalse(es.TryGet(ItemSubType.Weapon, out var slot));
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

            es.slots = new[] {_fx.GetComponent<EquipmentSlot>()};

            Assert.IsTrue(_fx.GetComponent<EquipmentSlots>().TryGet(ItemSubType.Armor, out var slot));
            Assert.AreEqual(ItemSubType.Armor, slot.itemSubType);
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
                "NotFoundComponentException`1: Not found `EquipSlot` component in MonoBehaviourTest: Tests.EquipmentSlotsFixture.");
            gameObject.AddComponent<EventTrigger>();
            var slot = gameObject.AddComponent<EquipmentSlot>();
            slot.itemSubType = ItemSubType.Armor;
            gameObject.AddComponent<EquipmentSlots>().slots = new [] {slot};
            IsTestFinished = true;
        }
    }
}
