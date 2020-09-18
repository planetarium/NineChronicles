using Nekoyume.Model.Item;
using System.Collections.Generic;

namespace Nekoyume
{
    public static class UnlockHelper
    {
        public static List<(ItemSubType, int)> GetAvailableEquipmentSlots(int level)
        {
            var availableSlots = new List<(ItemSubType, int)>();

            if (level >= GameConfig.RequireCharacterLevel.CharacterEquipmentSlotWeapon)
            {
                availableSlots.Add((ItemSubType.Weapon, 1));
            }
            if (level >= GameConfig.RequireCharacterLevel.CharacterEquipmentSlotArmor)
            {
                availableSlots.Add((ItemSubType.Armor, 1));
            }
            if (level >= GameConfig.RequireCharacterLevel.CharacterEquipmentSlotBelt)
            {
                availableSlots.Add((ItemSubType.Belt, 1));
            }
            if (level >= GameConfig.RequireCharacterLevel.CharacterEquipmentSlotNecklace)
            {
                availableSlots.Add((ItemSubType.Necklace, 1));
            }
            if (level >= GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing1)
            {
                if (level >= GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing2)
                {
                    availableSlots.Add((ItemSubType.Ring, 2));
                }
                else
                {
                    availableSlots.Add((ItemSubType.Ring, 1));
                }
            }

            return availableSlots;
        }
    }
}
