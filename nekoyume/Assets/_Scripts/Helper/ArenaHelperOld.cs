using System;
using System.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.State;
using Nekoyume.State;
using UnityEngine;

namespace Nekoyume
{
    /// <summary>
    /// This class will be removed after the beginning of the new arena system.
    /// </summary>
    public static class ArenaHelperOld
    {
        public static bool TryGetThisWeekAddress(out Address weeklyArenaAddress)
        {
            return TryGetThisWeekAddress(Game.Game.instance.Agent.BlockIndex, out weeklyArenaAddress);
        }

        public static bool TryGetThisWeekAddress(long blockIndex, out Address weeklyArenaAddress)
        {
            var gameConfigState = States.Instance.GameConfigState;
            var index = (int) blockIndex / gameConfigState.WeeklyArenaInterval;
            if (index < 0)
            {
                return false;
            }

            weeklyArenaAddress = WeeklyArenaState.DeriveAddress(index);
            return true;
        }

        public static async Task<WeeklyArenaState> GetThisWeekStateAsync(long blockIndex)
        {
            if (blockIndex != Game.Game.instance.Agent.BlockIndex)
            {
                NcDebug.LogError(
                    $"[{nameof(ArenaHelperOld)}.{nameof(GetThisWeekStateAsync)}] `{nameof(blockIndex)}`({blockIndex}) not equals with `Game.Game.instance.Agent.BlockIndex`({Game.Game.instance.Agent.BlockIndex})");
                return null;
            }

            if (!TryGetThisWeekAddress(blockIndex, out var address))
                return null;

            var state = await Game.Game.instance.Agent.GetStateAsync(
                ReservedAddresses.LegacyAccount,
                address);
            return state is null ? null : new WeeklyArenaState(state);
        }

        public static Address GetPrevWeekAddress()
        {
            return GetPrevWeekAddress(Game.Game.instance.Agent.BlockIndex);
        }

        public static Address GetPrevWeekAddress(long thisWeekBlockIndex)
        {
            var gameConfigState = States.Instance.GameConfigState;
            var index = Math.Max((int) thisWeekBlockIndex / gameConfigState.WeeklyArenaInterval, 0);
            index--;
            return WeeklyArenaState.DeriveAddress(index);
        }

        public static Address GetNextWeekAddress(long blockIndex)
        {
            var gameConfigState = States.Instance.GameConfigState;
            var index = (int) blockIndex / gameConfigState.WeeklyArenaInterval;
            index++;
            return WeeklyArenaState.DeriveAddress(index);
        }
    }
}
