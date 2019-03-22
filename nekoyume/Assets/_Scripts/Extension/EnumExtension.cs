using System;
using Nekoyume.Game.Item;

namespace Nekoyume
{
    public static class EnumExtension
    {
        public static ItemBase.ItemType ToEnumItemType(this string s)
        {
            ItemBase.ItemType result;
            Enum.TryParse(s, out result);
            return result;
        }
    }
}