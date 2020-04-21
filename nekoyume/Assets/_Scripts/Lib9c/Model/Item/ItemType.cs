using System.Collections.Generic;

namespace Nekoyume.Model.Item
{
    public enum ItemType
    {
        Consumable,
        Equipment,
        Material
    }

    public enum ItemSubType
    {
        // todo: Consumable or Consumable Material
        // Consumable
        Food,

        // todo: Equipment or Equipment Material
        // Equipment
        Weapon,
        Armor,
        Belt,
        Necklace,
        Ring,

        // todo: Remove `EquipmentMaterial` and `FoodMaterial`
        // todo: Material -> ETC
        // Material
        EquipmentMaterial,
        FoodMaterial,
        MonsterPart,
        NormalMaterial,
        Hourglass,
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
