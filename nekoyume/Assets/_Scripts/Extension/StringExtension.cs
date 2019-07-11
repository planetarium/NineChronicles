using System;
using Nekoyume.Game.Item;

namespace Nekoyume
{
    public static class StringExtension
    {
        private const string ExceptionFormat = "Invalid string. {0}";
        
        public static ItemBase.ItemType ToEnumItemType(this string s)
        {
            if (Enum.TryParse(s, out ItemBase.ItemType result))
            {
                return result;
            }
            
            throw new InvalidCastException(string.Format(ExceptionFormat, s));
        }

        public static string ToStatString(this string s)
        {
            switch (s)
            {
                case "damage":
                    return "공격력";
                case "defense":
                    return "방어력";
                case "health":
                    return "체력";
                case "luck":
                    return "행운";
                case "turnSpeed":
                    return "행동력";
                case "attackRange":
                    return "공격 거리";
                default:
                    throw new ArgumentOutOfRangeException(nameof(s), s, null);
            }
        }
    }
}
