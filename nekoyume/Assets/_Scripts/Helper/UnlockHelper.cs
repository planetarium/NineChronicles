using System.Collections.Generic;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Helper
{
    public static class UnlockHelper
    {
        public static List<(ItemSubType, int)> GetAvailableEquipmentSlots(
            int level,
            GameConfigState gameConfigState)
        {
            var availableSlots = new List<(ItemSubType, int)>();

            if (level >= gameConfigState.RequireCharacterLevel_EquipmentSlotWeapon)
            {
                availableSlots.Add((ItemSubType.Weapon, 1));
            }

            if (level >= gameConfigState.RequireCharacterLevel_EquipmentSlotArmor)
            {
                availableSlots.Add((ItemSubType.Armor, 1));
            }

            if (level >= gameConfigState.RequireCharacterLevel_EquipmentSlotBelt)
            {
                availableSlots.Add((ItemSubType.Belt, 1));
            }

            if (level >= gameConfigState.RequireCharacterLevel_EquipmentSlotNecklace)
            {
                availableSlots.Add((ItemSubType.Necklace, 1));
            }

            if (level >= gameConfigState.RequireCharacterLevel_EquipmentSlotRing1)
            {
                if (level >= gameConfigState.RequireCharacterLevel_EquipmentSlotRing2)
                {
                    availableSlots.Add((ItemSubType.Ring, 2));
                }
                else
                {
                    availableSlots.Add((ItemSubType.Ring, 1));
                }
            }

            if (level >= gameConfigState.RequireCharacterLevel_EquipmentSlotAura)
            {
                availableSlots.Add((ItemSubType.Aura, 1));
            }

            return availableSlots;
        }
    }
}
