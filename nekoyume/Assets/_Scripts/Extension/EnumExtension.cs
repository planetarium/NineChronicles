using System;
using Nekoyume.Game.Item;

namespace Nekoyume
{
    public static class EnumExtension
    {
        private const string ToEnumItemTypeException = "Invalid string.";
        
        public static ItemBase.ItemType ToEnumItemType(this string s)
        {
            ItemBase.ItemType result;
            if (Enum.TryParse(s, out result))
            {
                return result;
            }
            else
            {
                throw new InvalidCastException(ToEnumItemTypeException);   
            }
        }
    }
}
