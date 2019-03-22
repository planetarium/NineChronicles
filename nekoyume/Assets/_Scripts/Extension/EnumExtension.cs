using System;
using Nekoyume.Game.Item;

namespace Nekoyume
{
    public static class EnumExtension
    {
        private static readonly Type TypeOfItemType = typeof(ItemBase.ItemType);
        
        public static ItemBase.ItemType ToEnumItemType(this string s)
        {
            return (ItemBase.ItemType) Enum.Parse(TypeOfItemType, s);
        }
    }
}