using System;
using Nekoyume.Game.Item;

namespace Nekoyume
{
    public static class EnumExtension
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
    }
}
