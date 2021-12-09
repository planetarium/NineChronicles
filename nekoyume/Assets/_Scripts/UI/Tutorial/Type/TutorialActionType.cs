using System;
using System.Collections.Generic;

namespace Nekoyume.UI
{
    [Serializable]
    public enum TutorialActionType
    {
        None = 0,
        TutorialActionHackAndSlash = 1,
        TutorialActionGoToFirstRecipeCellView = 2,
        TutorialActionClickFirstRecipeCellView = 3,
        TutorialActionClickCombinationSubmitButton = 4,
        TutorialActionClickBottomMenuWorkShopButton = 5,
        TutorialActionClickBottomMenuMailButton = 6,
        TutorialActionClickBottomMenuCharacterButton = 7,
        TutorialActionCloseCombination = 8,
        TutorialActionClickFirstCombinationMailSubmitButton = 9,
        TutorialActionClickCombinationResultPopupSubmitButton = 10,
        TutorialActionClickAvatarInfoFirstInventoryCellView = 11,
        TutorialActionClickItemInformationTooltipSubmitButton = 12,
        TutorialActionCloseAvatarInfoWidget = 13,
        TutorialActionClickGuidedQuestWorldStage2 = 14,
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
