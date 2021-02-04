using System;
using System.Collections.Generic;

namespace Nekoyume.UI
{
    [Serializable]
    public enum TutorialActionType
    {
        None = 0,
        QuestClick,
    }

    public class TutorialActionTypeComparer : IEqualityComparer<TutorialActionType>
    {
        public bool Equals(TutorialActionType x, TutorialActionType y)
        {
            return x == y;
        }

        public int GetHashCode(TutorialActionType obj)
        {
            return obj.GetHashCode();
        }
    }
}
