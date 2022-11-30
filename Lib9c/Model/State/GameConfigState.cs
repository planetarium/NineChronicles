using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
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
#pragma warning disable LAA1002
            return new Dictionary(values.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        }

        public void Set(GameConfigSheet sheet)
        {
            foreach (var row in sheet.OrderedList)
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
            }
        }
    }
}
