using System;
using System.Collections.Generic;
using Nekoyume.L10n;

namespace Nekoyume.UI
{
    [Serializable]
    public enum TutorialTargetType
    {
        None = 0,
        HackAndSlashButton = 1, // QuestBtn
        CombinationButton = 2, // CombinationBtn
        CombineEquipmentCategoryButton = 3 // CombineEquipmentCategoryButton
    }

    public class TutorialTargetTypeComparer : IEqualityComparer<TutorialTargetType>
    {

        public bool Equals(TutorialTargetType x, TutorialTargetType y)
        {
            return x == y;
        }

        public int GetHashCode(TutorialTargetType obj)
        {
            return obj.GetHashCode();
        }
    }
}
