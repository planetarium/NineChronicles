using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    /// <summary>
    /// 이 객체는 외부 UI에 의해서 장비의 장착이나 해제 상태가 변하고 있음.
    /// 외부 UI에서는 항상 인벤토리와 함께 사용하고 있어서 그에 따르는 중복 코드가 생길 여지가 큼.
    /// UI간 결합을 끊고 이벤트 기반으로 동작하게끔 수정하면 좋겠음.
    /// </summary>
    public class EquipmentSlots : MonoBehaviour, IEnumerable<EquipmentSlot>
    {
        [SerializeField]
        private EquipmentSlot[] slots = null;

        private void Awake()
        {
            if (slots is null)
                throw new SerializeFieldNullException();
        }

        public void SetPlayer(Player player, Action<EquipmentSlot> onClick, Action<EquipmentSlot> onDoubleClick)
        {
            UpdateSlots(player.Level);
            foreach (var equipment in player.Equipments)
            {
                TryToEquip(equipment, onClick, onDoubleClick);
            }
        }

        public bool TryToEquip(Equipment equipment, Action<EquipmentSlot> onClick, Action<EquipmentSlot> onDoubleClick)
        {
            if (!TryGetToEquip(equipment, out var slot))
                return false;

            slot.Set(equipment, onClick, onDoubleClick);
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
            var typeSlots = slots.Where(e => !e.IsLock && e.ItemSubType == itemSubType).ToList();
            if (!typeSlots.Any())
            {
                slot = null;
                return false;
            }

            if (itemSubType == ItemSubType.Ring)
            {
                var itemId = equipment.ItemId;
                slot = typeSlots.FirstOrDefault(e => !e.IsEmpty && e.Item.ItemId.Equals(itemId))
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
            slot = slots.FirstOrDefault(e => !e.IsLock && !e.IsEmpty && e.Item.Equals(equipment));
            return slot;
        }

        /// <summary>
        /// 모든 슬롯을 해제한다.
        /// </summary>
        public void Clear()
        {
            foreach (var slot in slots)
            {
                slot.Clear();
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

        private void UpdateSlots(int avatarLevel)
        {
            foreach (var equipmentSlot in slots)
            {
                equipmentSlot.Set(avatarLevel);
            }
        }
    }
}
