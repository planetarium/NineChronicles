namespace Lib9c.Tests.Model.State
{
    using Bencodex.Types;
    using Lib9c.Tests.Fixtures.TableCSV;
    using Nekoyume;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class GameConfigStateTest
    {
        [Fact]
        public void Constructor_Empty()
        {
            var state = new GameConfigState();
            Assert.Equal(Addresses.GameConfig, state.address);
        }

        [Fact]
        public void Constructor_BencodexDictionary()
        {
            var serialized = GetDefaultGameConfigState().Serialize();
            var state = new GameConfigState((Dictionary)serialized);
            AssertDefaultGameConfigState(state);
        }

        [Fact]
        public void Serde()
        {
            var state = GetDefaultGameConfigState();
            var ser = state.Serialize();
            var des = new GameConfigState((Dictionary)ser);
            Assert.Equal(state.HourglassPerBlock, des.HourglassPerBlock);
            Assert.Equal(state.ActionPointMax, des.ActionPointMax);
            Assert.Equal(state.DailyRewardInterval, des.DailyRewardInterval);
            Assert.Equal(state.DailyArenaInterval, des.DailyArenaInterval);
            Assert.Equal(state.WeeklyArenaInterval, des.WeeklyArenaInterval);
            Assert.Equal(state.RequiredAppraiseBlock, des.RequiredAppraiseBlock);
            Assert.Equal(state.BattleArenaInterval, des.BattleArenaInterval);
            Assert.Equal(state.RuneStatSlotUnlockCost, des.RuneStatSlotUnlockCost);
            Assert.Equal(state.RuneSkillSlotUnlockCost, des.RuneSkillSlotUnlockCost);
            Assert.Equal(state.DailyRuneRewardAmount, des.DailyRuneRewardAmount);
            Assert.Equal(state.DailyWorldBossInterval, des.DailyWorldBossInterval);
            Assert.Equal(state.WorldBossRequiredInterval, des.WorldBossRequiredInterval);
            Assert.Equal(
                state.StakeRegularFixedRewardSheet_V2_StartBlockIndex,
                des.StakeRegularFixedRewardSheet_V2_StartBlockIndex);
            Assert.Equal(
                state.StakeRegularRewardSheet_V2_StartBlockIndex,
                des.StakeRegularRewardSheet_V2_StartBlockIndex);
            Assert.Equal(
                state.StakeRegularRewardSheet_V3_StartBlockIndex,
                des.StakeRegularRewardSheet_V3_StartBlockIndex);
            Assert.Equal(
                state.StakeRegularRewardSheet_V4_StartBlockIndex,
                des.StakeRegularRewardSheet_V4_StartBlockIndex);
            Assert.Equal(
                state.StakeRegularRewardSheet_V5_StartBlockIndex,
                des.StakeRegularRewardSheet_V5_StartBlockIndex);
            var ser2 = des.Serialize();
            Assert.Equal(ser, ser2);
        }

        [Fact]
        public void Set()
        {
            var state = new GameConfigState();
            state.Set(GetDefaultGameConfigSheet());
            AssertDefaultGameConfigState(state);
        }

        [Fact]
        public void Update()
        {
            var state = GetDefaultGameConfigState();
            Assert.Equal(3, state.HourglassPerBlock);
            var row = new GameConfigSheet.Row();
            row.Set(new[] { "hourglass_per_block", "5" });
            state.Update(row);
            Assert.Equal(5, state.HourglassPerBlock);
        }

        private static void AssertDefaultGameConfigState(GameConfigState state)
        {
            Assert.Equal(3, state.HourglassPerBlock);
            Assert.Equal(120, state.ActionPointMax);
            Assert.Equal(1700, state.DailyRewardInterval);
            Assert.Equal(5040, state.DailyArenaInterval);
            Assert.Equal(56000, state.WeeklyArenaInterval);
            Assert.Equal(10, state.RequiredAppraiseBlock);
            Assert.Equal(4, state.BattleArenaInterval);
            Assert.Equal(100, state.RuneStatSlotUnlockCost);
            Assert.Equal(1000, state.RuneSkillSlotUnlockCost);
            Assert.Equal(1, state.DailyRuneRewardAmount);
            Assert.Equal(7200, state.DailyWorldBossInterval);
            Assert.Equal(5, state.WorldBossRequiredInterval);
            Assert.Equal(6_700_000L, state.StakeRegularFixedRewardSheet_V2_StartBlockIndex);
            Assert.Equal(5_510_416L, state.StakeRegularRewardSheet_V2_StartBlockIndex);
            Assert.Equal(6_700_000L, state.StakeRegularRewardSheet_V3_StartBlockIndex);
            Assert.Equal(6_910_000L, state.StakeRegularRewardSheet_V4_StartBlockIndex);
            Assert.Equal(7_650_000L, state.StakeRegularRewardSheet_V5_StartBlockIndex);

            Assert.Equal(GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot, state.RequireCharacterLevel_FullCostumeSlot);
            Assert.Equal(GameConfig.RequireCharacterLevel.CharacterHairCostumeSlot, state.RequireCharacterLevel_HairCostumeSlot);
            Assert.Equal(GameConfig.RequireCharacterLevel.CharacterEarCostumeSlot, state.RequireCharacterLevel_EarCostumeSlot);
            Assert.Equal(GameConfig.RequireCharacterLevel.CharacterEyeCostumeSlot, state.RequireCharacterLevel_EyeCostumeSlot);
            Assert.Equal(GameConfig.RequireCharacterLevel.CharacterTailCostumeSlot, state.RequireCharacterLevel_TailCostumeSlot);
            Assert.Equal(GameConfig.RequireCharacterLevel.CharacterTitleSlot, state.RequireCharacterLevel_TitleSlot);

            Assert.Equal(GameConfig.RequireCharacterLevel.CharacterEquipmentSlotWeapon, state.RequireCharacterLevel_EquipmentSlotWeapon);
            Assert.Equal(GameConfig.RequireCharacterLevel.CharacterEquipmentSlotArmor, state.RequireCharacterLevel_EquipmentSlotArmor);
            Assert.Equal(GameConfig.RequireCharacterLevel.CharacterEquipmentSlotBelt, state.RequireCharacterLevel_EquipmentSlotBelt);
            Assert.Equal(GameConfig.RequireCharacterLevel.CharacterEquipmentSlotNecklace, state.RequireCharacterLevel_EquipmentSlotNecklace);
            Assert.Equal(GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing1, state.RequireCharacterLevel_EquipmentSlotRing1);
            Assert.Equal(GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing2, state.RequireCharacterLevel_EquipmentSlotRing2);
            Assert.Equal(GameConfig.RequireCharacterLevel.CharacterEquipmentSlotAura, state.RequireCharacterLevel_EquipmentSlotAura);

            Assert.Equal(GameConfig.RequireCharacterLevel.CharacterConsumableSlot1, state.RequireCharacterLevel_ConsumableSlot1);
            Assert.Equal(GameConfig.RequireCharacterLevel.CharacterConsumableSlot2, state.RequireCharacterLevel_ConsumableSlot2);
            Assert.Equal(GameConfig.RequireCharacterLevel.CharacterConsumableSlot3, state.RequireCharacterLevel_ConsumableSlot3);
            Assert.Equal(GameConfig.RequireCharacterLevel.CharacterConsumableSlot4, state.RequireCharacterLevel_ConsumableSlot4);
            Assert.Equal(GameConfig.RequireCharacterLevel.CharacterConsumableSlot5, state.RequireCharacterLevel_ConsumableSlot5);
        }

        private static GameConfigSheet GetDefaultGameConfigSheet()
        {
            var sheet = new GameConfigSheet();
            sheet.Set(GameConfigSheetFixtures.Default);
            return sheet;
        }

        private static GameConfigState GetDefaultGameConfigState()
        {
            var state = new GameConfigState();
            state.Set(GetDefaultGameConfigSheet());
            return state;
        }
    }
}
