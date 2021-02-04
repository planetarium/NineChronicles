using System;
using System.Collections.Generic;

namespace Nekoyume.UI
{
    [Serializable]
    public enum TutorialTargetType
    {
        None = 0,
        HackAndSlashButton, // 확인
        CombinationButton, // 확인
        CombineEquipmentCategoryButton, // 확인
        CombineEquipmentCategory,//
        WeaponTabButton,// 좀더 키워야됨
        CombinationSlotsButton,
        MailButton,// 확인
        SubmitWithCostButton, //
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
