#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.TableData;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class GameConfigState : State
    {
        public static readonly Address Address = Addresses.GameConfig;
        public int HourglassPerBlock { get; private set; }
        public int ActionPointMax { get; private set; }
        public int DailyRewardInterval { get; private set; }
        public int DailyArenaInterval { get; private set; }
        public int WeeklyArenaInterval { get; private set; }
        public int RequiredAppraiseBlock { get; private set; }
        public int BattleArenaInterval { get; private set; }
        public int RuneStatSlotUnlockCost { get; private set; }
        public int RuneSkillSlotUnlockCost { get; private set; }
        public int DailyRuneRewardAmount { get; private set; }
        public int DailyWorldBossInterval { get; private set; }
        public int WorldBossRequiredInterval { get; private set; }
        public long StakeRegularFixedRewardSheet_V2_StartBlockIndex { get; private set; }
        public long StakeRegularRewardSheet_V2_StartBlockIndex { get; private set; }
        public long StakeRegularRewardSheet_V3_StartBlockIndex { get; private set; }
        public long StakeRegularRewardSheet_V4_StartBlockIndex { get; private set; }
        public long StakeRegularRewardSheet_V5_StartBlockIndex { get; private set; }

        public int RequireCharacterLevel_FullCostumeSlot { get; private set; }
        public int RequireCharacterLevel_HairCostumeSlot { get; private set; }
        public int RequireCharacterLevel_EarCostumeSlot { get; private set; }
        public int RequireCharacterLevel_EyeCostumeSlot { get; private set; }
        public int RequireCharacterLevel_TailCostumeSlot { get; private set; }
        public int RequireCharacterLevel_TitleSlot { get; private set; }

        public int RequireCharacterLevel_EquipmentSlotWeapon { get; private set; }
        public int RequireCharacterLevel_EquipmentSlotArmor { get; private set; }
        public int RequireCharacterLevel_EquipmentSlotBelt { get; private set; }
        public int RequireCharacterLevel_EquipmentSlotNecklace { get; private set; }
        public int RequireCharacterLevel_EquipmentSlotRing1 { get; private set; }
        public int RequireCharacterLevel_EquipmentSlotRing2 { get; private set; }
        public int RequireCharacterLevel_EquipmentSlotAura { get; private set; }

        public int RequireCharacterLevel_ConsumableSlot1 { get; private set; }
        public int RequireCharacterLevel_ConsumableSlot2 { get; private set; }
        public int RequireCharacterLevel_ConsumableSlot3 { get; private set; }
        public int RequireCharacterLevel_ConsumableSlot4 { get; private set; }
        public int RequireCharacterLevel_ConsumableSlot5 { get; private set; }

        public GameConfigState() : base(Address)
        {
        }

        public GameConfigState(Dictionary serialized) : base(serialized)
        {
            if (serialized.TryGetValue((Text) "hourglass_per_block", out var value))
            {
                HourglassPerBlock = value.ToInteger();
            }
            if (serialized.TryGetValue((Text) "action_point_max", out var value2))
            {
                ActionPointMax = value2.ToInteger();
            }
            if (serialized.TryGetValue((Text) "daily_reward_interval", out var value3))
            {
                DailyRewardInterval = value3.ToInteger();
            }
            if (serialized.TryGetValue((Text) "daily_arena_interval", out var value4))
            {
                DailyArenaInterval = value4.ToInteger();
            }
            if (serialized.TryGetValue((Text) "weekly_arena_interval", out var value5))
            {
                WeeklyArenaInterval = value5.ToInteger();
            }
            if (serialized.TryGetValue((Text)"required_appraise_block", out var value6))
            {
                RequiredAppraiseBlock = value6.ToInteger();
            }
            if (serialized.TryGetValue((Text)"battle_arena_interval", out var value7))
            {
                BattleArenaInterval = value7.ToInteger();
            }
            if (serialized.TryGetValue((Text)"rune_stat_slot_unlock_cost", out var value8))
            {
                RuneStatSlotUnlockCost = value8.ToInteger();
            }
            if (serialized.TryGetValue((Text)"rune_skill_slot_unlock_cost", out var value9))
            {
                RuneSkillSlotUnlockCost = value9.ToInteger();
            }
            if (serialized.TryGetValue((Text)"daily_rune_reward_amount", out var value10))
            {
                DailyRuneRewardAmount = value10.ToInteger();
            }
            if (serialized.TryGetValue((Text)"daily_worldboss_interval", out var value11))
            {
                DailyWorldBossInterval = value11.ToInteger();
            }
            if (serialized.TryGetValue((Text)"worldboss_required_interval", out var value12))
            {
                WorldBossRequiredInterval = value12.ToInteger();
            }
            if (serialized.TryGetValue(
                    (Text)"stake_regular_fixed_reward_sheet_v2_start_block_index",
                    out var stakeRegularFixedRewardSheetV2StartBlockIndex))
            {
                StakeRegularFixedRewardSheet_V2_StartBlockIndex =
                    stakeRegularFixedRewardSheetV2StartBlockIndex.ToLong();
            }
            if (serialized.TryGetValue(
                    (Text)"stake_regular_reward_sheet_v2_start_block_index",
                    out var stakeRegularRewardSheetV2StartBlockIndex))
            {
                StakeRegularRewardSheet_V2_StartBlockIndex =
                    stakeRegularRewardSheetV2StartBlockIndex.ToLong();
            }
            if (serialized.TryGetValue(
                    (Text)"stake_regular_reward_sheet_v3_start_block_index",
                    out var stakeRegularRewardSheetV3StartBlockIndex))
            {
                StakeRegularRewardSheet_V3_StartBlockIndex =
                    stakeRegularRewardSheetV3StartBlockIndex.ToLong();
            }
            if (serialized.TryGetValue(
                    (Text)"stake_regular_reward_sheet_v4_start_block_index",
                    out var stakeRegularRewardSheetV4StartBlockIndex))
            {
                StakeRegularRewardSheet_V4_StartBlockIndex =
                    stakeRegularRewardSheetV4StartBlockIndex.ToLong();
            }
            if (serialized.TryGetValue(
                    (Text)"stake_regular_reward_sheet_v5_start_block_index",
                    out var stakeRegularRewardSheetV5StartBlockIndex))
            {
                StakeRegularRewardSheet_V5_StartBlockIndex =
                    stakeRegularRewardSheetV5StartBlockIndex.ToLong();
            }

            if (serialized.TryGetValue((Text)"character_full_costume_slot",
                    out var characterFullCostumeSlot))
            {
                RequireCharacterLevel_FullCostumeSlot = characterFullCostumeSlot.ToInteger();
            }
            if (serialized.TryGetValue((Text)"character_hair_costume_slot",
                    out var characterHairCostumeSlot))
            {
                RequireCharacterLevel_HairCostumeSlot = characterHairCostumeSlot.ToInteger();
            }
            if (serialized.TryGetValue((Text)"character_ear_costume_slot",
                    out var characterEarCostumeSlot))
            {
                RequireCharacterLevel_EarCostumeSlot = characterEarCostumeSlot.ToInteger();
            }
            if (serialized.TryGetValue((Text)"character_eye_costume_slot",
                    out var characterEyeCostumeSlot))
            {
                RequireCharacterLevel_EyeCostumeSlot = characterEyeCostumeSlot.ToInteger();
            }
            if (serialized.TryGetValue((Text)"character_tail_costume_slot",
                    out var characterTailCostumeSlot))
            {
                RequireCharacterLevel_TailCostumeSlot = characterTailCostumeSlot.ToInteger();
            }
            if (serialized.TryGetValue((Text)"character_title_costume_slot",
                    out var characterTitleSlot))
            {
                RequireCharacterLevel_TitleSlot = characterTitleSlot.ToInteger();
            }

            if (serialized.TryGetValue((Text)"character_equipment_slot_weapon",
                    out var characterEquipmentSlotWeapon))
            {
                RequireCharacterLevel_EquipmentSlotWeapon = characterEquipmentSlotWeapon.ToInteger();
            }
            if (serialized.TryGetValue((Text)"character_equipment_slot_armor",
                    out var characterEquipmentSlotArmor))
            {
                RequireCharacterLevel_EquipmentSlotArmor = characterEquipmentSlotArmor.ToInteger();
            }
            if (serialized.TryGetValue((Text)"character_equipment_slot_belt", out var characterEquipmentSlotBelt))
            {
                RequireCharacterLevel_EquipmentSlotBelt = characterEquipmentSlotBelt.ToInteger();
            }
            if (serialized.TryGetValue((Text)"character_equipment_slot_necklace",
                    out var characterEquipmentSlotNecklace))
            {
                RequireCharacterLevel_EquipmentSlotNecklace = characterEquipmentSlotNecklace.ToInteger();
            }
            if (serialized.TryGetValue((Text)"character_equipment_slot_ring1",
                    out var characterEquipmentSlotRing1))
            {
                RequireCharacterLevel_EquipmentSlotRing1 = characterEquipmentSlotRing1.ToInteger();
            }
            if (serialized.TryGetValue((Text)"character_equipment_slot_ring2",
                    out var characterEquipmentSlotRing2))
            {
                RequireCharacterLevel_EquipmentSlotRing2 = characterEquipmentSlotRing2.ToInteger();
            }
            if (serialized.TryGetValue((Text)"character_equipment_slot_aura",
                    out var characterEquipmentSlotAura))
            {
                RequireCharacterLevel_EquipmentSlotAura = characterEquipmentSlotAura.ToInteger();
            }

            if (serialized.TryGetValue((Text)"character_consumable_slot_1",
                    out var characterConsumableSlot1))
            {
                RequireCharacterLevel_ConsumableSlot1 = characterConsumableSlot1.ToInteger();
            }
            if (serialized.TryGetValue((Text)"character_consumable_slot_2",
                    out var characterConsumableSlot2))
            {
                RequireCharacterLevel_ConsumableSlot2 = characterConsumableSlot2.ToInteger();
            }
            if (serialized.TryGetValue((Text)"character_consumable_slot_3",
                    out var characterConsumableSlot3))
            {
                RequireCharacterLevel_ConsumableSlot3 = characterConsumableSlot3.ToInteger();
            }
            if (serialized.TryGetValue((Text)"character_consumable_slot_4",
                    out var characterConsumableSlot4))
            {
                RequireCharacterLevel_ConsumableSlot4 = characterConsumableSlot4.ToInteger();
            }
            if (serialized.TryGetValue((Text)"character_consumable_slot_5",
                    out var characterConsumableSlot5))
            {
                RequireCharacterLevel_ConsumableSlot5 = characterConsumableSlot5.ToInteger();
            }

        }

        public GameConfigState(string csv) : base(Address)
        {
            var sheet = new GameConfigSheet();
            sheet.Set(csv);
            foreach (var row in sheet.Values)
            {
                Update(row);
            }
        }

        public override IValue Serialize()
        {
            var values = new Dictionary<IKey, IValue>
            {
                [(Text) "hourglass_per_block"] = HourglassPerBlock.Serialize(),
                [(Text) "action_point_max"] = ActionPointMax.Serialize(),
                [(Text) "daily_reward_interval"] = DailyRewardInterval.Serialize(),
                [(Text) "daily_arena_interval"] = DailyArenaInterval.Serialize(),
                [(Text) "weekly_arena_interval"] = WeeklyArenaInterval.Serialize(),
                [(Text) "required_appraise_block"] = RequiredAppraiseBlock.Serialize(),
            };
            if (BattleArenaInterval > 0)
            {
                values.Add((Text)"battle_arena_interval", BattleArenaInterval.Serialize());
            }

            if (RuneStatSlotUnlockCost > 0)
            {
                values.Add((Text)"rune_stat_slot_unlock_cost", RuneStatSlotUnlockCost.Serialize());
            }

            if (RuneSkillSlotUnlockCost > 0)
            {
                values.Add((Text)"rune_skill_slot_unlock_cost", RuneSkillSlotUnlockCost.Serialize());
            }

            if (DailyRuneRewardAmount > 0)
            {
                values.Add((Text)"daily_rune_reward_amount", DailyRuneRewardAmount.Serialize());
            }

            if (DailyWorldBossInterval > 0)
            {
                values.Add((Text)"daily_worldboss_interval", DailyWorldBossInterval.Serialize());
            }

            if (WorldBossRequiredInterval > 0)
            {
                values.Add((Text)"worldboss_required_interval", WorldBossRequiredInterval.Serialize());
            }

            if (StakeRegularFixedRewardSheet_V2_StartBlockIndex > 0)
            {
                values.Add(
                    (Text) "stake_regular_fixed_reward_sheet_v2_start_block_index",
                    StakeRegularFixedRewardSheet_V2_StartBlockIndex.Serialize());
            }

            if (StakeRegularRewardSheet_V2_StartBlockIndex > 0)
            {
                values.Add(
                    (Text) "stake_regular_reward_sheet_v2_start_block_index",
                    StakeRegularRewardSheet_V2_StartBlockIndex.Serialize());
            }

            if (StakeRegularRewardSheet_V3_StartBlockIndex > 0)
            {
                values.Add(
                    (Text) "stake_regular_reward_sheet_v3_start_block_index",
                    StakeRegularRewardSheet_V3_StartBlockIndex.Serialize());
            }

            if (StakeRegularRewardSheet_V4_StartBlockIndex > 0)
            {
                values.Add(
                    (Text) "stake_regular_reward_sheet_v4_start_block_index",
                    StakeRegularRewardSheet_V4_StartBlockIndex.Serialize());
            }

            if (StakeRegularRewardSheet_V5_StartBlockIndex > 0)
            {
                values.Add(
                    (Text) "stake_regular_reward_sheet_v5_start_block_index",
                    StakeRegularRewardSheet_V5_StartBlockIndex.Serialize());
            }

            if (RequireCharacterLevel_FullCostumeSlot > 0)
            {
                values.Add(
                    (Text)"character_full_costume_slot",
                    RequireCharacterLevel_FullCostumeSlot.Serialize());
            }

            if (RequireCharacterLevel_HairCostumeSlot > 0)
            {
                    values.Add(
                    (Text)"character_hair_costume_slot",
                    RequireCharacterLevel_HairCostumeSlot.Serialize());
            }

            if (RequireCharacterLevel_EarCostumeSlot > 0)
            {
                values.Add(
                    (Text)"character_ear_costume_slot",
                    RequireCharacterLevel_EarCostumeSlot.Serialize());
            }

            if (RequireCharacterLevel_EyeCostumeSlot > 0)
            {
                    values.Add(
                    (Text)"character_eye_costume_slot",
                    RequireCharacterLevel_EyeCostumeSlot.Serialize());
            }

            if (RequireCharacterLevel_TailCostumeSlot > 0)
            {
                    values.Add(
                    (Text)"character_tail_costume_slot",
                    RequireCharacterLevel_TailCostumeSlot.Serialize());
            }

            if (RequireCharacterLevel_TitleSlot > 0)
            {
                values.Add(
                    (Text)"character_title_costume_slot",
                    RequireCharacterLevel_TitleSlot.Serialize());
            }

            if (RequireCharacterLevel_EquipmentSlotWeapon > 0)
            {
                values.Add(
                    (Text)"character_equipment_slot_weapon",
                    RequireCharacterLevel_EquipmentSlotWeapon.Serialize());
            }

            if (RequireCharacterLevel_EquipmentSlotArmor > 0)
            {
                values.Add(
                    (Text)"character_equipment_slot_armor",
                    RequireCharacterLevel_EquipmentSlotArmor.Serialize());
            }

            if (RequireCharacterLevel_EquipmentSlotBelt > 0)
            {
                values.Add(
                    (Text)"character_equipment_slot_belt",
                    RequireCharacterLevel_EquipmentSlotBelt.Serialize());
            }

            if (RequireCharacterLevel_EquipmentSlotNecklace > 0)
            {
                values.Add(
                    (Text)"character_equipment_slot_necklace",
                    RequireCharacterLevel_EquipmentSlotNecklace.Serialize());
            }

            if (RequireCharacterLevel_EquipmentSlotRing1 > 0)
            {
                values.Add(
                    (Text)"character_equipment_slot_ring1",
                    RequireCharacterLevel_EquipmentSlotRing1.Serialize());
            }

            if (RequireCharacterLevel_EquipmentSlotRing2 > 0)
            {
                values.Add(
                    (Text)"character_equipment_slot_ring2",
                    RequireCharacterLevel_EquipmentSlotRing2.Serialize());
            }

            if (RequireCharacterLevel_EquipmentSlotAura > 0)
            {
                values.Add(
                    (Text)"character_equipment_slot_aura",
                    RequireCharacterLevel_EquipmentSlotAura.Serialize());
            }

            if (RequireCharacterLevel_ConsumableSlot1 > 0)
            {
                values.Add(
                    (Text)"character_consumable_slot_1",
                    RequireCharacterLevel_ConsumableSlot1.Serialize());
            }

            if (RequireCharacterLevel_ConsumableSlot2 > 0)
            {
                    values.Add(
                    (Text)"character_consumable_slot_2",
                    RequireCharacterLevel_ConsumableSlot2.Serialize());
            }

            if (RequireCharacterLevel_ConsumableSlot3 > 0)
            {
                    values.Add(
                    (Text)"character_consumable_slot_3",
                    RequireCharacterLevel_ConsumableSlot3.Serialize());
            }

            if (RequireCharacterLevel_ConsumableSlot4 > 0)
            {
                    values.Add(
                    (Text)"character_consumable_slot_4",
                    RequireCharacterLevel_ConsumableSlot4.Serialize());
            }

            if (RequireCharacterLevel_ConsumableSlot5 > 0)
            {
                values.Add(
                    (Text)"character_consumable_slot_5",
                    RequireCharacterLevel_ConsumableSlot5.Serialize());
            }

#pragma warning disable LAA1002
            return new Dictionary(values.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        }

        public void Set(GameConfigSheet sheet)
        {
            if (sheet.OrderedList is not { } orderedList)
            {
                throw new NullReferenceException(nameof(sheet.OrderedList));
            }

            foreach (var row in orderedList)
            {
                Update(row);
            }
        }

        public void Update(GameConfigSheet.Row row)
        {
            switch (row.Key)
            {
                case "hourglass_per_block":
                    HourglassPerBlock = TableExtensions.ParseInt(row.Value);
                    break;
                case "action_point_max":
                    ActionPointMax = TableExtensions.ParseInt(row.Value);
                    break;
                case "daily_reward_interval":
                    DailyRewardInterval = TableExtensions.ParseInt(row.Value);
                    break;
                case "daily_arena_interval":
                    DailyArenaInterval = TableExtensions.ParseInt(row.Value);
                    break;
                case "weekly_arena_interval":
                    WeeklyArenaInterval = TableExtensions.ParseInt(row.Value);
                    break;
                case "required_appraise_block":
                    RequiredAppraiseBlock = TableExtensions.ParseInt(row.Value);
                    break;
                case "battle_arena_interval":
                    BattleArenaInterval = TableExtensions.ParseInt(row.Value);
                    break;
                case "rune_stat_slot_unlock_cost":
                    RuneStatSlotUnlockCost = TableExtensions.ParseInt(row.Value);
                    break;
                case "rune_skill_slot_unlock_cost":
                    RuneSkillSlotUnlockCost = TableExtensions.ParseInt(row.Value);
                    break;
                case "daily_rune_reward_amount":
                    DailyRuneRewardAmount = TableExtensions.ParseInt(row.Value);
                    break;
                case "daily_worldboss_interval":
                    DailyWorldBossInterval = TableExtensions.ParseInt(row.Value);
                    break;
                case "worldboss_required_interval":
                    WorldBossRequiredInterval = TableExtensions.ParseInt(row.Value);
                    break;
                case "stake_regular_fixed_reward_sheet_v2_start_block_index":
                    StakeRegularFixedRewardSheet_V2_StartBlockIndex =
                        TableExtensions.ParseLong(row.Value);
                    break;
                case "stake_regular_reward_sheet_v2_start_block_index":
                    StakeRegularRewardSheet_V2_StartBlockIndex =
                        TableExtensions.ParseLong(row.Value);
                    break;
                case "stake_regular_reward_sheet_v3_start_block_index":
                    StakeRegularRewardSheet_V3_StartBlockIndex =
                        TableExtensions.ParseLong(row.Value);
                    break;
                case "stake_regular_reward_sheet_v4_start_block_index":
                    StakeRegularRewardSheet_V4_StartBlockIndex =
                        TableExtensions.ParseLong(row.Value);
                    break;
                case "stake_regular_reward_sheet_v5_start_block_index":
                    StakeRegularRewardSheet_V5_StartBlockIndex =
                        TableExtensions.ParseLong(row.Value);
                    break;

                case "character_full_costume_slot":
                    RequireCharacterLevel_FullCostumeSlot =
                        TableExtensions.ParseInt(row.Value);
                    break;
                case "character_hair_costume_slot":
                    RequireCharacterLevel_HairCostumeSlot =
                        TableExtensions.ParseInt(row.Value);
                    break;
                case "character_ear_costume_slot":
                    RequireCharacterLevel_EarCostumeSlot =
                        TableExtensions.ParseInt(row.Value);
                    break;
                case "character_eye_costume_slot":
                    RequireCharacterLevel_EyeCostumeSlot =
                        TableExtensions.ParseInt(row.Value);
                    break;
                case "character_tail_costume_slot":
                    RequireCharacterLevel_TailCostumeSlot =
                        TableExtensions.ParseInt(row.Value);
                    break;
                case "character_title_costume_slot":
                    RequireCharacterLevel_TitleSlot =
                        TableExtensions.ParseInt(row.Value);
                    break;

                case "character_equipment_slot_weapon":
                    RequireCharacterLevel_EquipmentSlotWeapon =
                        TableExtensions.ParseInt(row.Value);
                    break;
                case "character_equipment_slot_armor":
                    RequireCharacterLevel_EquipmentSlotArmor =
                        TableExtensions.ParseInt(row.Value);
                    break;
                case "character_equipment_slot_belt":
                    RequireCharacterLevel_EquipmentSlotBelt =
                        TableExtensions.ParseInt(row.Value);
                    break;
                case "character_equipment_slot_necklace":
                    RequireCharacterLevel_EquipmentSlotNecklace =
                        TableExtensions.ParseInt(row.Value);
                    break;
                case "character_equipment_slot_ring1":
                    RequireCharacterLevel_EquipmentSlotRing1 =
                        TableExtensions.ParseInt(row.Value);
                    break;
                case "character_equipment_slot_ring2":
                    RequireCharacterLevel_EquipmentSlotRing2 =
                        TableExtensions.ParseInt(row.Value);
                    break;
                case "character_equipment_slot_aura":
                    RequireCharacterLevel_EquipmentSlotAura =
                        TableExtensions.ParseInt(row.Value);
                    break;

                case "character_consumable_slot_1":
                    RequireCharacterLevel_ConsumableSlot1 =
                            TableExtensions.ParseInt(row.Value);
                    break;
                case "character_consumable_slot_2":
                    RequireCharacterLevel_ConsumableSlot2 =
                            TableExtensions.ParseInt(row.Value);
                    break;
                case "character_consumable_slot_3":
                    RequireCharacterLevel_ConsumableSlot3 =
                            TableExtensions.ParseInt(row.Value);
                    break;
                case "character_consumable_slot_4":
                    RequireCharacterLevel_ConsumableSlot4 =
                            TableExtensions.ParseInt(row.Value);
                    break;
                case "character_consumable_slot_5":
                    RequireCharacterLevel_ConsumableSlot5 =
                            TableExtensions.ParseInt(row.Value);
                    break;

            }
        }
    }
}
