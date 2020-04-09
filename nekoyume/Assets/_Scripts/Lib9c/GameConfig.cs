namespace Nekoyume
{
    public static class GameConfig
    {
#if UNITY_EDITOR
        public const bool IsEditor = true;
#else
        public const bool IsEditor = false;
#endif

        public const int SlotCount = 3;
        public const float WaitSeconds = 180f;
        public const string AvatarNickNamePattern = @"^[0-9가-힣a-zA-Z]{2,20}$";
        public const string UnicodePattern = @"[^\u0000-\u007F]";
        public const string DiscordLink = "https://discord.gg/NqshYve";

        public const string BlockExplorerLinkFormat =
            "http://explorer.libplanet.io/9c-alpha/account/?{0}";

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
        public const int ActionPointMax = 120;

        public const int DailyRewardInterval = 2300;
        public const int DailyArenaInterval = 500;
        public const int WeeklyArenaInterval = 8000 * 7;

        //TODO 안쓰는 프리팹과 함께 삭제해야함
        public const int CombineEquipmentCostAP = 5;
        public const int CombineEquipmentCostNCG = 10;
        public const int EnhanceEquipmentCostAP = 5;
        public const int EnhanceEquipmentCostNCG = 10;

        public const int RankingRewardFirst = 50;
        public const int RankingRewardSecond = 30;
        public const int RankingRewardThird = 10;

        public const int ArenaActivationCostNCG = 100;
        public const int ArenaScoreDefault = 1000;
        public const int ArenaChallengeCountMax = 5;

        #endregion

        #region system or contents unlock

        public static class RequireCharacterLevel
        {
            #region character equipment slot

            public const int CharacterEquipmentSlotWeapon = 1;
            public const int CharacterEquipmentSlotArmor = IsEditor ? 1 : 5;
            public const int CharacterEquipmentSlotBelt = IsEditor ? 1 : 9;
            public const int CharacterEquipmentSlotNecklace = IsEditor ? 1 : 15;
            public const int CharacterEquipmentSlotRing1 = IsEditor ? 1 : 20;
            public const int CharacterEquipmentSlotRing2 = IsEditor ? 1 : 50;

            #endregion

            #region character consumable slot

            public const int CharacterConsumableSlot1 = 1;
            public const int CharacterConsumableSlot2 = IsEditor ? 1 : 10;
            public const int CharacterConsumableSlot3 = IsEditor ? 1 : 20;
            public const int CharacterConsumableSlot4 = IsEditor ? 1 : 50;
            public const int CharacterConsumableSlot5 = IsEditor ? 1 : 100;

            #endregion
        }

        public static class RequireClearedStageLevel
        {
            #region action

            public const int CombinationEquipmentAction = IsEditor ? 1 : 3;
            public const int CombinationConsumableAction = IsEditor ? 1 : 6;
            public const int ItemEnhancementAction = IsEditor ? 1 : 9;
            public const int ActionsInShop = IsEditor ? 1 : 17;
            public const int ActionsInRankingBoard = IsEditor ? 1 : 25;

            #endregion

            #region ui

            public const int UIMainMenuStage = 0;
            public const int UIMainMenuCombination = CombinationEquipmentAction;
            public const int UIMainMenuShop = ActionsInShop;
            public const int UIMainMenuRankingBoard = ActionsInRankingBoard;

            public const int UIBottomMenuInBattle = 1;
            public const int UIBottomMenuCharacter = 1;
            public const int UIBottomMenuInventory = 1;
            public const int UIBottomMenuSettings = 1;
            public const int UIBottomMenuMail = IsEditor ? 1 : 3;
            public const int UIBottomMenuChat = IsEditor ? 1 : 7;
            public const int UIBottomMenuQuest = IsEditor ? 1 : 9;

            #endregion
        }

        #endregion
    }
}
