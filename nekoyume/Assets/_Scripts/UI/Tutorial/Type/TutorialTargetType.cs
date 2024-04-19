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
        CombinationResultPopupButton = 18, // 좌표 안맞음
        MenuButton = 19,
        InventorySecondCell = 20,
        // 가독성을 더하기 위해 튜토리얼이 진행되는 stage id * 10000 부터 1씩 더함
        PortalReward = 70001,
        ItemTooltipEquipButton = 30000,
        BattlePreparationStartButton = 30001,
        SummonEnteringButton = 50000,  // Combination -> Summon
        Normal1SummonButton,
        PatrolRewardMenu = 100000,
        PatrolTimeText,
        ClaimPatrolRewardButton,
        // TutorialScenario 편집을 json에서 SO로 바꿨기 때문에 Enum 값에 가독성이 필요없어짐
	    // Arena - 15
	    ArenaMenu = 1000000,
	    ArenaSeasonText,
	    ArenaTicket,
	    // Upgrade - 35
	    CombinationUpgradeButton, // Combination -> Upgrade
	    CombinationInventoryFirstCell,
	    CombinationInventorySecondCell,
	    CombinationInventoryGaugebar,
	    CombinationDeleteButton,
	    // Grind - 40
	    CombinationGrindButton, // Combination -> Grind
	    // Craft Recipe - Premium and SuperCraft - 45
	    CombineEquipmentSuperCraftGauge,
	    CombineEquipmentSuperCraft,
	    CombineEquipmentSuperCraftPopupClose,
	    CombineEquipmentPremiumRecipeButton,
	    // WorldBoss - 50
	    WorldBossButton,
	    WorldBossSeasonInformation,
	    WorldBossSeasonRewardsButton,
	    WorldBossRewardsButton,
	    WorldBossEnterPracticeButton,
        // Action point and rune stone - 23
        ActionPointHeaderMenu,
        ActionPointChargeButton,
        CombinationRuneButton, // Combination -> Rune
        CombinationRuneCombineButton,
        CombinationRuneLevelBonusArea
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
