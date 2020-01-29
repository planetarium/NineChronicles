using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Model.Item;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class EquipmentSlots : MonoBehaviour, IEnumerable<EquipmentSlot>
    {
        public EquipmentSlot[] slots;

        private void Awake()
        {
            if (slots is null)
                throw new NotFoundComponentException<EquipmentSlot>(gameObject);
        }

        public bool TryGet(ItemSubType type, out EquipmentSlot slot)
        {
            if (type == ItemSubType.Ring)
            {
                slot = slots.FirstOrDefault(es => es.itemSubType == ItemSubType.Ring && es.item?.Data is null)
                       ?? slots.First(es => es.itemSubType == ItemSubType.Ring);
                return slot;
            }

            slot = slots.FirstOrDefault(es => es.itemSubType == type);
            return slot;
        }

        public EquipmentSlot FindSlotWithItem(ItemUsable item)
        {
            foreach (var slot in slots)
            {
                if (item.Equals(slot.item))
                {
                    return slot;
                }
            }
            return null;
        }

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
