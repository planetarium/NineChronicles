using System;
using System.Collections.Generic;

namespace Nekoyume.Model.Item
{
    public enum ItemType
    {
        Consumable = 0,
        Costume = 1,
        Equipment = 2,
        Material = 3,
    }

    public enum ItemSubType
    {
        // Consumable
        Food = 0,

        // Costume
        FullCostume = 1,
        HairCostume = 2,
        EarCostume = 3,
        EyeCostume = 4,
        TailCostume = 5,

        // Equipment
        Weapon = 6,
        Armor = 7,
        Belt = 8,
        Necklace = 9,
        Ring = 10,

        // Material
        EquipmentMaterial = 11,
        FoodMaterial = 12,
        MonsterPart = 13,
        NormalMaterial = 14,
        Hourglass = 15,
        ApStone = 16,
        [Obsolete("ItemSubType.Chest has never been used outside the MaterialItemSheet. And we won't use it in the future until we have a specific reason.")]
        Chest = 17,

        // Costume
        Title = 18,
    }

    public class ItemTypeComparer : IEqualityComparer<ItemType>
    {
        public static readonly ItemTypeComparer Instance = new ItemTypeComparer();

        public bool Equals(ItemType x, ItemType y)
        {
            return x == y;
        }

        public int GetHashCode(ItemType obj)
        {
            return (int) obj;
        }
    }

    public class ItemSubTypeComparer : IEqualityComparer<ItemSubType>
    {
        public static readonly ItemSubTypeComparer Instance = new ItemSubTypeComparer();

        public bool Equals(ItemSubType x, ItemSubType y)
        {
            return x == y;
        }

        public int GetHashCode(ItemSubType obj)
        {
            return (int) obj;
        }
    }
}
