using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        public bool TryGet(ItemBase.ItemType type, out EquipSlot slot)
        {
            if (type == ItemBase.ItemType.Ring)
            {
                slot = slots.FirstOrDefault(es =>
                           es.type == ItemBase.ItemType.Ring && es.item?.Data is null)
                       ?? slots.First(es => es.type == ItemBase.ItemType.Ring);
                return slot;
            }

            slot = slots.FirstOrDefault(es => es.type == type);
            return slot;
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
