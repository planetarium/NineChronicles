using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nekoyume.Model.Item;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class EquipmentSlots : MonoBehaviour, IEnumerable<EquipmentSlot>
    {
        [SerializeField]
        private EquipmentSlot[] slots = null;

        private void Awake()
        {
            if (slots is null)
                throw new SerializeFieldNullException();
        }
        
        public bool TryToEquip(Equipment equipment, Action<EquipmentSlot> onClick, Action<EquipmentSlot> onDoubleClick, bool throwException = true)
        {
            if (!TryGetToEquip(equipment, out var slot))
            {
                if (!throwException)
                    return false;
                
                var sb = new StringBuilder();
                sb.Append($"[{nameof(StatusDetail)}] failed to equip {nameof(equipment)}.");
                sb.Append($" / {nameof(equipment.Data.Id)} {equipment.Data.Id}");
                sb.Append($" / {nameof(equipment.Data.ItemType)} {equipment.Data.ItemType}");
                sb.Append($" / {nameof(equipment.Data.ItemSubType)} {equipment.Data.ItemSubType}");
                throw new Exception(sb.ToString());
            }
            
            slot.Set(equipment);
            slot.SetOnClickAction(onClick, onDoubleClick);
            return true;
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
        
        #region IEnumerable<EquipmentSlot>

        public IEnumerator<EquipmentSlot> GetEnumerator()
        {
            return slots.ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        #endregion
    }
}
