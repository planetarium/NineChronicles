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

            [Obsolete("Use GameConfigState.RequireCharacterLevel_FullCostumeSlot")]
            public const int CharacterFullCostumeSlot = 2;
            [Obsolete("Use GameConfigState.RequireCharacterLevel_HairCostumeSlot")]
            public const int CharacterHairCostumeSlot = 2;
            [Obsolete("Use GameConfigState.RequireCharacterLevel_EarCostumeSlot")]
            public const int CharacterEarCostumeSlot = 2;
            [Obsolete("Use GameConfigState.RequireCharacterLevel_EyeCostumeSlot")]
            public const int CharacterEyeCostumeSlot = 2;
            [Obsolete("Use GameConfigState.RequireCharacterLevel_TailCostumeSlot")]
            public const int CharacterTailCostumeSlot = 2;
            [Obsolete("Use GameConfigState.RequireCharacterLevel_TitleSlot")]
            public const int CharacterTitleSlot = 1;

            #endregion

            #region character equipment slot

            [Obsolete("Use GameConfigState.RequireCharacterLevel_EquipmentSlotWeapon")]
            public const int CharacterEquipmentSlotWeapon = 1;
            [Obsolete("Use GameConfigState.RequireCharacterLevel_EquipmentSlotArmor")]
            public const int CharacterEquipmentSlotArmor = 3;
            [Obsolete("Use GameConfigState.RequireCharacterLevel_EquipmentSlotBelt")]
            public const int CharacterEquipmentSlotBelt = 5;
            [Obsolete("Use GameConfigState.RequireCharacterLevel_EquipmentSlotNecklace")]
            public const int CharacterEquipmentSlotNecklace = 8;
            [Obsolete("Use GameConfigState.RequireCharacterLevel_EquipmentSlotRing1")]
            public const int CharacterEquipmentSlotRing1 = 13;
            [Obsolete("Use GameConfigState.RequireCharacterLevel_EquipmentSlotRing2")]
            public const int CharacterEquipmentSlotRing2 = 46;
            [Obsolete("Use GameConfigState.RequireCharacterLevel_EquipmentSlotAura")]
            public const int CharacterEquipmentSlotAura = 1;

            #endregion

            #region character consumable slot

            [Obsolete("Use GameConfigState.RequireCharacterLevel_ConsumableSlot1")]
            public const int CharacterConsumableSlot1 = 1;
            [Obsolete("Use GameConfigState.RequireCharacterLevel_ConsumableSlot2")]
            public const int CharacterConsumableSlot2 = 35;
            [Obsolete("Use GameConfigState.RequireCharacterLevel_ConsumableSlot3")]
            public const int CharacterConsumableSlot3 = 100;
            [Obsolete("Use GameConfigState.RequireCharacterLevel_ConsumableSlot4")]
            public const int CharacterConsumableSlot4 = 200;
            [Obsolete("Use GameConfigState.RequireCharacterLevel_ConsumableSlot5")]
            public const int CharacterConsumableSlot5 = 350;

            #endregion
        }

        public static class RequireClearedStageLevel
        {
            #region action

            [Obsolete("Not used anymore since v200092")]
            public const int CombinationEquipmentAction = 3;
            [Obsolete("Not used anymore since v200092")]
            public const int CombinationConsumableAction = 6;
            [Obsolete("Not used anymore since v200092")]
            public const int ItemEnhancementAction = 9;
            [Obsolete("Not used anymore since v200092")]
            public const int ActionsInShop = 17;
            [Obsolete("Not used anymore since v200092")]
            public const int ActionsInRankingBoard = 25;
            [Obsolete("Not used anymore since v200092")]
            public const int ActionsInMimisbrunnr = 100;
            [Obsolete("Not used anymore since v200092")]
            public const int ActionsInRaid = 50;

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
            public const int Aura = 1;
        }
    }
}
