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

        // 가독성을 더하기 위해 튜토리얼이 진행되는 stage id * 10000 부터 1씩 더함
        TutorialActionClickPortalRewardButton = 70001,
        TutorialActionClickBattlePreparationFirstInventoryCellView = 30000,
        TutorialActionClickBattlePreparationHackAndSlash = 30001,
        TutorialActionGoToWorkShop = 50000,
        TutorialActionClickSummonEnteringButton,
        TutorialActionClickNormal1SummonButton,
        TutorialActionClickBattlePreparationSecondInventoryCellView,
        TutorialActionClickPatrolRewardMenu = 100000,
        TutorialActionClickClaimPatrolRewardButton,

        // TutorialScenario 편집을 json에서 SO로 바꿨기 때문에 Enum 값에 가독성이 필요없어짐
        // Arena
        TutorialActionClickArenaMenu = 1000000,

        // Upgrade
        TutorialActionClickCombinationUpgradeButton,
        TutorialActionClickCombinationInventoryFirstCell,
        TutorialActionClickCombinationInventorySecondCell,
        TutorialActionClickCombinationDeleteButton,

        // Grind
        TutorialActionClickCombinationGrindButton,

        // Craft Recipe - Premium and SuperCraft
        TutorialActionClickCombineEquipmentSuperCraft,
        TutorialActionClickCombineEquipmentSuperCraftPopupClose,
        TutorialActionClickCombineEquipmentPremiumRecipeButton,

        // World
        TutorialActionClickWorldBossButton,
        TutorialActionClickWorldBossSeasonRewardsButton,
        TutorialActionClickWorldBossRewardsButton,
        TutorialActionClickWorldBossEnterPracticeButton,
        TutorialActionCloseWorldBossDetail,

        // Action point and rune stone - 23
        TutorialActionClickCombinationRuneButton,
        TutorialActionClickCombinationRuneCombineButton,
        TutorialActionActionPointHeaderMenu,
        TutorialActionActionPointChargeButton,

        // SeasonPass
        TutorialActionSeasonPassGuidePopup
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
