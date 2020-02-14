using System;
using Libplanet;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume
{
    public static class ArenaHelper
    {
        public static bool TryGetThisWeekAddress(out Address weeklyArenaAddress)
        {
            return TryGetThisWeekAddress(Game.Game.instance.Agent.BlockIndex, out weeklyArenaAddress);
        }

        public static bool TryGetThisWeekAddress(long blockIndex, out Address weeklyArenaAddress)
        {
            var index = (int) blockIndex / GameConfig.WeeklyArenaInterval;
            if (index < 0 ||
                index >= WeeklyArenaState.Addresses.Count)
                return false;

            weeklyArenaAddress = WeeklyArenaState.Addresses[index];
            return true;
        }

        public static bool TryGetThisWeekState(out WeeklyArenaState weeklyArenaState)
        {
            return TryGetThisWeekState(Game.Game.instance.Agent.BlockIndex, out weeklyArenaState);
        }

        public static bool TryGetThisWeekState(long blockIndex, out WeeklyArenaState weeklyArenaState)
        {
            weeklyArenaState = null;
            if (blockIndex != Game.Game.instance.Agent.BlockIndex)
            {
                Debug.LogError(
                    $"[{nameof(ArenaHelper)}.{nameof(TryGetThisWeekState)}] `{nameof(blockIndex)}`({blockIndex}) not equals with `Game.Game.instance.Agent.BlockIndex`({Game.Game.instance.Agent.BlockIndex})");
                return false;
            }

            if (!TryGetThisWeekAddress(blockIndex, out var address))
                return false;

            weeklyArenaState = new WeeklyArenaState(Game.Game.instance.Agent.GetState(address));
            return true;
        }

        public static Address GetPrevWeekAddress()
        {
            return GetPrevWeekAddress(Game.Game.instance.Agent.BlockIndex);
        }
        
        public static Address GetPrevWeekAddress(long thisWeekBlockIndex)
        {
            var index = Math.Max((int) thisWeekBlockIndex / GameConfig.WeeklyArenaInterval, 0);
            return WeeklyArenaState.Addresses[index];
        }

        public static bool TryGetThisWeekStateAndArenaInfo(Address avatarAddress, out WeeklyArenaState weeklyArenaState,
            out ArenaInfo arenaInfo)
        {
            return TryGetThisWeekStateAndArenaInfo(Game.Game.instance.Agent.BlockIndex, avatarAddress,
                out weeklyArenaState, out arenaInfo);
        }

        public static bool TryGetThisWeekStateAndArenaInfo(long blockIndex, Address avatarAddress,
            out WeeklyArenaState weeklyArenaState,
            out ArenaInfo arenaInfo)
        {
            arenaInfo = null;
            return TryGetThisWeekState(blockIndex, out weeklyArenaState) &&
                   weeklyArenaState.TryGetValue(avatarAddress, out arenaInfo);
        }
    }
}
