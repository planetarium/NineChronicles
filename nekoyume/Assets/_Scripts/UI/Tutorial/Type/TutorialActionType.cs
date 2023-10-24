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
        TutorialActionClickMenuButton = 15,
        TutorialActionClickPortalRewardButton = 70001,
        TutorialActionClickBattlePreparationFirstInventoryCellView = 30000,
        TutorialActionClickBattlePreparationHackAndSlash = 30001,
        TutorialActionGoToWorkShop = 50000,
        TutorialActionClickSummonEnteringButton,
        TutorialActionClickNormal1SummonButton,
        TutorialActionClickPatrolRewardMenu = 100000,
        TutorialActionClickClaimPatrolRewardButton,
	    //아레나
	    TutorialActionClickArenaMenu = 1000000,
	    //업그레이드
	    TutorialActionClickCombinationUpgradeButton,
	    TutorialActionClickCombinationInventoryFirstCell,
	    TutorialActionClickCombinationInventorySecondCell,
	    TutorialActionClickCombinationDeleteButton,
	    //그라인드
	    TutorialActionClickCombinationGrindButton,
	    //슈퍼크래프트와 프리미엄
	    TutorialActionClickCombineEquipmentSuperCraft,
	    TutorialActionClickCombineEquipmentSuperCraftPopupClose,
	    TutorialActionClickCombineEquipmentPremiumRecipeButton,
	    //월드보스
	    TutorialActionClickWorldBossButton,
	    TutorialActionClickWorldBossSeasonRewardsButton,
	    TutorialActionClickWorldBossRewardsButton,
	    TutorialActionClickWorldBossEnterPracticeButton,
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
