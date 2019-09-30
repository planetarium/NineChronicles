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
        // Consumable
        Food,
        
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
        
        // Material
        EquipmentMaterial,
        FoodMaterial,
        MonsterPart,
        NormalMaterial
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
