using System;
using System.Collections.Generic;

namespace Nekoyume.UI
{
    [Serializable]
    public enum TutorialTargetType // 순서 유지해줘야됨
    {
        None = 0,
        HackAndSlashButton = 1,
        CombinationButton = 2,
        CombineEquipmentCategoryButton = 3,
        CombineEquipmentCategory = 4 , // size 안맞음
        WeaponTabButton = 5,
        CombinationSlotsButton = 6,
        MailButton = 7,
        CombineWithCostButton = 8, // <---- 더미데이터 써야할듯
        EquipmentRecipeCellView = 9, //
        InventoryFirstCell = 10, //
        InventoryEquipWeapon = 11, //
        CharacterButton = 12, //
        BackButton = 13, //
        ItemEquipButton = 14, // 사이즈큼
        MailReceiveButton = 15, //
        CombinationSlots = 16, //// <---- 더미데이터 써야할듯 -340 / +50, +90
        GuidedQuestButton = 17, // 좌표 안맞음
        CombinationResultPopupButton = 18// 좌표 안맞음
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
