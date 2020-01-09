using System.Collections.Generic;
using Assets.SimpleLocalization;

namespace Nekoyume.EnumType
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
        RangedWeapon,
        Armor,
        Belt,
        Necklace,
        Ring,
        Helm,
        Set,
        Shoes,
        
        // todo: Remove `EquipmentMaterial` and `FoodMaterial`
        // todo: Material -> ETC
        // Material
        EquipmentMaterial,
        FoodMaterial,
        MonsterPart,
        NormalMaterial
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

    public static class ItemTypeExtension
    {
        public static string GetLocalizedString(this ItemType value)
        {
            return LocalizationManager.Localize($"ITEM_TYPE_{value}");
        }
        
        public static string GetLocalizedString(this ItemSubType value)
        {
            return LocalizationManager.Localize($"ITEM_SUB_TYPE_{value}");
        }
    }
}
