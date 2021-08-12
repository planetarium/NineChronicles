using System;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Model.State;
using Nekoyume.State;
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
            var gameConfigState = States.Instance.GameConfigState;
            var index = (int) blockIndex / gameConfigState.WeeklyArenaInterval;
            if (index < 0)
            {
                return false;
            }

            weeklyArenaAddress = WeeklyArenaState2.DeriveAddress(index);
            return true;
        }

        public static bool TryGetThisWeekState(out WeeklyArenaState2 weeklyArenaState)
        {
            return TryGetThisWeekState(Game.Game.instance.Agent.BlockIndex, out weeklyArenaState);
        }

        public static bool TryGetThisWeekState(long blockIndex, out WeeklyArenaState2 weeklyArenaState)
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

            weeklyArenaState = new WeeklyArenaState2((List)Game.Game.instance.Agent.GetState(address));
            return true;
        }

        public static Address GetPrevWeekAddress()
        {
            return GetPrevWeekAddress(Game.Game.instance.Agent.BlockIndex);
        }
        
        public static Address GetPrevWeekAddress(long thisWeekBlockIndex)
        {
            var gameConfigState = States.Instance.GameConfigState;
            var index = Math.Max((int) thisWeekBlockIndex / gameConfigState.WeeklyArenaInterval, 0);
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
