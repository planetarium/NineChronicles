using System;
using System.Collections.Generic;

namespace Nekoyume.UI
{
    [Serializable]
    public enum TutorialTargetType // 순서 유지해줘야됨
    {
        None = 0,
        HackAndSlashButton,
        CombinationButton,
        CombineEquipmentCategoryButton,
        CombineEquipmentCategory, // size 안맞음
        WeaponTabButton,
        CombinationSlotsButton,
        MailButton,
        CombineWithCostButton, // <---- 더미데이터 써야할듯
        EquipmentRecipeCellView, //
        InventoryFirstCell, //
        InventoryEquipWeapon, //
        CharacterButton, //
        BackButton, //
        ItemEquipButton, // 사이즈큼
        MailReceiveButton, //
        CombinationSlots, //// <---- 더미데이터 써야할듯 -340 / +50, +90
        GuidedQuestButton, // 좌표 안맞음
        CombinationResultPopupButton// 좌표 안맞음
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
