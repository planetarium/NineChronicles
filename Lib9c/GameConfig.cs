using System;

namespace Nekoyume
{
    public static class GameConfig
    {
#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
        public const bool IsEditor = true;
#else
        public const bool IsEditor = false;
#endif

        public const int SlotCount = 3;
        public const float WaitSeconds = 180f;
        public const string AvatarNickNamePattern = @"^[0-9a-zA-Z]{2,20}$";
        public const string DiscordLink = "https://discord.gg/NqshYve";

        public const string BlockExplorerLinkFormat =
            "http://explorer.libplanet.io/9c-beta/account/?{0}";

        public const float PlayerSpeechBreakTime = 2f;
        public const int MimisbrunnrWorldId = 10001;
        public const int MimisbrunnrStartStageId = 10000001;
        public const int DefaultAttackId = 100000;

        //TODO 온체인으로 옮겨야함.
        // re: 그렇네요. 가장 확인된 방법은 테이블로 빼는 방법이네요.

        #region action

        public const int DefaultAvatarCharacterId = 100010;
        public const int DefaultAvatarWeaponId = 10100000;
        public const int DefaultAvatarArmorId = 10200000;

        //TODO 안쓰는 프리팹과 함께 삭제해야함
        public const int CombineEquipmentCostAP = 5;
        public const int CombineEquipmentCostNCG = 10;
        public const int EnhanceEquipmentCostAP = 0;

        public const int RankingRewardFirst = 50;
        public const int RankingRewardSecond = 30;
        public const int RankingRewardThird = 10;

        public const int ArenaActivationCostNCG = 100;
        public const int ArenaScoreDefault = 1000;
        public const int ArenaChallengeCountMax = 5;
        public const int MaximumProbability = 10000;
        [Obsolete("Use GameConfigState.RequiredAppraiseBlock")]
        public const int RequiredAppraiseBlock = 50;

        #endregion

        #region system or contents unlock

        public static class RequireCharacterLevel
        {
            #region character costume slot

            public const int CharacterFullCostumeSlot = IsEditor ? 1 : 2;
            public const int CharacterHairCostumeSlot = IsEditor ? 1 : 2;
            public const int CharacterEarCostumeSlot = IsEditor ? 1 : 2;
            public const int CharacterEyeCostumeSlot = IsEditor ? 1 : 2;
            public const int CharacterTailCostumeSlot = IsEditor ? 1 : 2;
            public const int CharacterTitleSlot = 1;

            #endregion

            #region character equipment slot

            public const int CharacterEquipmentSlotWeapon = 1;
            public const int CharacterEquipmentSlotArmor = IsEditor ? 1 : 3;
            public const int CharacterEquipmentSlotBelt = IsEditor ? 1 : 5;
            public const int CharacterEquipmentSlotNecklace = IsEditor ? 1 : 8;
            public const int CharacterEquipmentSlotRing1 = IsEditor ? 1 : 13;
            public const int CharacterEquipmentSlotRing2 = IsEditor ? 1 : 46;

            #endregion

            #region character consumable slot

            public const int CharacterConsumableSlot1 = 1;
            public const int CharacterConsumableSlot2 = IsEditor ? 1 : 35;
            public const int CharacterConsumableSlot3 = IsEditor ? 1 : 100;
            public const int CharacterConsumableSlot4 = IsEditor ? 1 : 200;
            public const int CharacterConsumableSlot5 = IsEditor ? 1 : 350;

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
            public const int ActionsInMimisbrunnr = IsEditor ? 1 : 100;
            public const int ActionsInRaid = IsEditor ? 1 : 50;

            #endregion

            #region ui

            public const int UIMainMenuStage = 0;
            public const int UIMainMenuCombination = CombinationEquipmentAction;
            public const int UIMainMenuShop = ActionsInShop;
            public const int UIMainMenuRankingBoard = ActionsInRankingBoard;
            public const int UIMainMenuMimisbrunnr = ActionsInMimisbrunnr;

            public const int UIBottomMenuInBattle = 1;
            public const int UIBottomMenuCharacter = 1;
            public const int UIBottomMenuMail = CombinationEquipmentAction;
            public const int UIBottomMenuChat = IsEditor ? 1 : 7;
            public const int UIBottomMenuQuest = 1;
            public const int UIBottomMenuMimisbrunnr = ActionsInMimisbrunnr;
            public const int UIBottomMenuRanking = IsEditor ? 1 : 3;

            #endregion
        }

        #endregion

        public static class MaxEquipmentSlotCount
        {
            public const int Weapon = 1;
            public const int Armor = 1;
            public const int Belt = 1;
            public const int Necklace = 1;
            public const int Ring = 2;
        }
    }
}
