using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class EquipmentSlots : MonoBehaviour, IEnumerable<EquipmentSlot>
    {
        [SerializeField]
        private EquipmentSlot[] slots = null;

        private void Awake()
        {
            if (slots is null)
                throw new NotFoundComponentException<EquipmentSlot>(gameObject);
        }

        /// <summary>
        /// `equipment`를 장착하기 위한 슬롯을 반환한다.
        /// 반지의 경우 이미 장착되어 있는 슬롯이 있다면 이를 반환한다.
        /// </summary>
        /// <param name="equipment"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        public bool TryGetToEquip(Equipment equipment, out EquipmentSlot slot)
        {
            var itemSubType = equipment.Data.ItemSubType;
            var typeSlots = slots.Where(e => e.itemSubType == itemSubType).ToList();
            if (!typeSlots.Any())
            {
                slot = null;
                return false;
            }

            if (itemSubType == ItemSubType.Ring)
            {
                var itemId = equipment.ItemId;
                slot = typeSlots.FirstOrDefault(e => !e.IsEmpty && e.item.ItemId.Equals(itemId))
                       ?? typeSlots.FirstOrDefault(e => e.IsEmpty)
                       ?? typeSlots.First();
            }
            else
            {
                slot = typeSlots.First();
            }

            return true;
        }

        /// <summary>
        /// `equipment`가 이미 장착되어 있는 슬롯을 반환한다.
        /// </summary>
        /// <param name="equipment"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        public bool TryGetAlreadyEquip(Equipment equipment, out EquipmentSlot slot)
        {
            slot = slots.FirstOrDefault(e => !e.IsEmpty && e.item.Equals(equipment));
            return slot;
        }

        /// <summary>
        /// 모든 슬롯을 해제한다.
        /// </summary>
        public void Clear()
        {
            foreach (var slot in slots)
            {
                slot.Unequip();
            }
        }

        public IEnumerator<EquipmentSlot> GetEnumerator()
        {
            return slots.ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
