using System;
using System.Collections.Generic;

namespace Nekoyume.UI
{
    [Serializable]
    public enum GuideType
    {
        None = 0,
        Square,
        Circle,
        Outline,
        Stop,
        End,
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
