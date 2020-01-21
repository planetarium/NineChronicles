using Libplanet;
using Nekoyume.State;

namespace Nekoyume
{
    public static class ArenaHelper
    {
        public static bool TryGetThisWeekAddress(out Address weeklyArenaAddress)
        {
            var index = (int) Game.Game.instance.Agent.blockIndex.Value / GameConfig.WeeklyArenaInterval;
            if (index < 0 ||
                index >= WeeklyArenaState.Addresses.Count)
                return false;

            weeklyArenaAddress = WeeklyArenaState.Addresses[index];
            return true;
        }
        
        public static bool TryGetThisWeekState(out WeeklyArenaState weeklyArenaState)
        {
            weeklyArenaState = null;
            if (!TryGetThisWeekAddress(out var address))
                return false;
            
            weeklyArenaState = new WeeklyArenaState(Game.Game.instance.Agent.GetState(address));
            return true;
        }
    }
}
