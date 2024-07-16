using System;
using System.Collections.Generic;

namespace Nekoyume.UI
{
    [Serializable]
    public enum DialogPositionType
    {
        None = 0,
        Top = 1,
        Bottom = 2
    }

    public class DialogPositionTypeEqualityComparer : IEqualityComparer<DialogPositionType>
    {
        public bool Equals(DialogPositionType x, DialogPositionType y)
        {
            return x == y;
        }

        public int GetHashCode(DialogPositionType obj)
        {
            return (int)obj;
        }
    }
}
