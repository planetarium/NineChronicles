using Nekoyume.Helper;
using UnityEngine;

namespace Nekoyume
{
    public static class GameConfig
    {
        public const int SlotCount = 3;
        public const float WaitSeconds = 180f;
        public const string AvatarNickNamePattern = @"^[0-9가-힣a-zA-Z]{2,20}$";
        public const string UnicodePattern = @"[^\u0000-\u007F]";
        public const string DiscordLink = "https://discord.gg/NqshYve";
        public const string BlockExplorerLinkFormat = "http://explorer.libplanet.io/9c-alpha/account/?{0}";
        public const float PlayerSpeechBreakTime = 2f;

        //TODO 온체인으로 옮겨야함.
        // re: 그렇네요. 가장 확인된 방법은 테이블로 빼는 방법이네요.
        #region action

        public const int DefaultAvatarCharacterId = 100010;
        public const int DefaultAvatarWeaponId = 10100000;
        public const int DefaultAvatarArmorId = 10200000;
        public const string DefaultPlayerEarLeftResource = "ear_0001_left";
        public const string DefaultPlayerEarRightResource = "ear_0001_right";
        public const string DefaultPlayerEyeOpenResource = "eye_red_open";
        public const string DefaultPlayerEyeHalfResource = "eye_red_half";
        public const string DefaultPlayerTailResource = "tail_0001";
        public const int ActionPoint = 250;

        public const int DailyRewardInterval = 1000;
        public const int DailyArenaInterval = 500; // 8000
        public const int WeeklyArenaInterval = DailyArenaInterval * 3; // 7
        public const int BaseVictoryPoint = 20;
        public const int BaseDefeatPoint = -15;

        public const int HackAndSlashCostAP = 5;

        public const int CombineConsumableCostAP = 5;
        public const int CombineEquipmentCostAP = 5;
        public const int CombineEquipmentCostNCG = 10;
        public const int EnhanceEquipmentCostAP = 5;
        public const int EnhanceEquipmentCostNCG = 10;
        public const decimal CombinationValueP1 = 3m; // 30f;
        public const decimal CombinationValueP2 = 1m; // 1.2f;
        public const decimal CombinationValueL1 = 10m;
        public const decimal CombinationValueL2 = 1m;
        public const decimal CombinationValueR1 = 1.5m; // 2f;
        public const int CombinationDefaultFoodId = 200000;

#if UNITY_EDITOR
        public const int QuestRequiredLevel = 1;
        public const int CombinationRequiredLevel = 1;
        public const int ShopRequiredLevel = 1;
        public const int RankingRequiredLevel = 1;
#else
        public const int QuestRequiredLevel = 1;
        public const int CombinationRequiredLevel = 3;
        public const int ShopRequiredLevel = 7;
        public const int RankingRequiredLevel = 5;
#endif

        #endregion

        #region CP

        public const float CPNormalAttackMultiply = 1f;
        public const float CPBlowAttackMultiply = 1.1f;
        public const float CPBlowAllAttackMultiply = 1.15f;
        public const float CPDoubleAttackMultiply = 1.15f;
        public const float CPAreaAttackMultiply = 1.2f;
        public const float CPHealMultiply = 1.1f;
        public const float CPBuffMultiply = 1.1f;
        public const float CPDebuffMultiply = 1.1f;
        
        #endregion
        
        #region Color
        
        public const string ColorHexForGrade1 = "fff9dd";
        public const string ColorHexForGrade2 = "12ff00";
        public const string ColorHexForGrade3 = "0f91ff";
        public const string ColorHexForGrade4 = "ffae00";
        public const string ColorHexForGrade5 = "ba00ff";

        public static readonly Color ColorForGrade1 = ColorHelper.HexToColorRGB(ColorHexForGrade1);
        public static readonly Color ColorForGrade2 = ColorHelper.HexToColorRGB(ColorHexForGrade2);
        public static readonly Color ColorForGrade3 = ColorHelper.HexToColorRGB(ColorHexForGrade3);
        public static readonly Color ColorForGrade4 = ColorHelper.HexToColorRGB(ColorHexForGrade4);
        public static readonly Color ColorForGrade5 = ColorHelper.HexToColorRGB(ColorHexForGrade5);

        #endregion
    }
}
