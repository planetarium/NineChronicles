using System;
using Nekoyume.Game.Item;

namespace Nekoyume
{
    public static class TypeExtensions
    {
        public static bool IsInheritsFrom(this Type source, Type destination)
        {
            var t = source;
            while (t != null)
            {
                if (t == destination)
                {
                    return true;
                }

                t = t.BaseType;
            }

            return false;
        }
    }
}
