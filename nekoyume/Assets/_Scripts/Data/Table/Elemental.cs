using System;

namespace Nekoyume.Data.Table
{
    [Serializable]
    public class Elemental : Row
    {
        // 0 : Normal
        // 1 : Fire
        // 2 : Land
        // 3 : Water
        // 4 : Wind
        public enum ElementalType
        {
            Normal,
            Fire,
            Land,
            Water,
            Wind,
        }

        public ElementalType id;
        public ElementalType strong;
        public ElementalType weak;
        public float multiply = 0.0f;

        public static string GetDescription(ElementalType type)
        {
            switch (type)
            {
                case ElementalType.Normal:
                    return "무속성";
                case ElementalType.Fire:
                    return "불속성";
                case ElementalType.Land:
                    return "땅속성";
                case ElementalType.Water:
                    return "물속성";
                case ElementalType.Wind:
                    return "바람속성";
                default:
                    throw new Game.Elemental.InvalidElementalException();
            }
        }
    }
}
