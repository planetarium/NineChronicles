using System.Collections.Generic;

namespace Nekoyume.UI
{
    public enum GuideType
    {
        None = 0,
        Square = 1,
        Circle = 2,
        Stop = 3,
        End = 4,
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
