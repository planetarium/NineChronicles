using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class EquipmentSlots : MonoBehaviour, IEnumerable<EquipSlot>
    {
        public EquipSlot[] slots;
        

        private void Awake()
        {
            if (slots is null)
                throw new NotFoundComponentException<EquipSlot>(gameObject);
        }

        public bool TryGet(ItemSubType type, out EquipSlot slot)
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

        public EquipSlot FindSlotWithItem(ItemUsable item)
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

        public IEnumerator<EquipSlot> GetEnumerator()
        {
            return slots.ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
