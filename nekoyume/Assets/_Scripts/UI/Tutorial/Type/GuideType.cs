using System;
using System.Collections.Generic;

namespace Nekoyume.UI
{
    [Serializable]
    public enum GuideType
    {
        None = 0,
        Square = 1,
        Circle = 2,
        Outline = 3,
        Stop = 4,
        End = 5,
    }

    public class GuideTypeEqualityComparer : IEqualityComparer<GuideType>
    {
        public bool Equals(GuideType x, GuideType y)
        {
            return x == y;
        }

        public int GetHashCode(GuideType obj)
        {
            return (int) obj;
        }
    }
}
