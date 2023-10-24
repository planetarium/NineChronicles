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
        PortalReward = 70001,
        ItemTooltipEquipButton = 30000,
        BattlePreparationStartButton = 30001,
        SummonEnteringButton = 50000,
        Normal1SummonButton,
        PatrolRewardMenu = 100000,
        PatrolTimeText,
        ClaimPatrolRewardButton,
	//아레나
	ArenaMenu = 150000,
	ArenaSeasonText = 150001,
	ArenaTicket = 150002,
	//업그레이드
	CombinationUpgradeButton = 350000,
	CombinationInventoryFirstCell = 350001,
	CombinationInventorySecondCell = 350002,
	CombinationInventoryGaugebar = 350003,
	CombinationDeleteButton = 350004,
	//그라인드
	CombinationGrindButton = 400001,
	//프리미엄과 슈퍼크래프트
	CombineEquipmentSuperCraftGauge = 450000,
	CombineEquipmentSuperCraft = 450001,
	CombineEquipmentSuperCraftPopupClose = 450002,
	CombineEquipmentPremiumRecipeButton = 450003,
	//월드보스
	WorldBossButton = 500000,
	WorldBossSeasonInformation = 500001,
	WorldBossSeasonRewardsButton = 500002,
	WorldBossRewardsButton = 500002,
	WorldBossEnterPracticeButton = 500003,
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
