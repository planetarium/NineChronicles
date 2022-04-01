using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Model.State;

namespace Nekoyume.UI.Model
{
    public class ArenaInfoList
    {
        private Dictionary<Address, ArenaInfo> _map = new Dictionary<Address, ArenaInfo>();

        private WeeklyArenaState _cachedState;

        public bool Locked;

        public List<ArenaInfo> OrderedArenaInfos = new List<ArenaInfo>();

        public void Update(WeeklyArenaState weeklyArenaState, bool @lock)
        {
            _cachedState = weeklyArenaState;
            foreach (var kv in _cachedState.Map)
            {
                _map[kv.Key] = kv.Value.State;
            }
            OrderedArenaInfos = _map.Values
                .OrderByDescending(i => i.Score)
                .ThenBy(i => i.CombatPoint)
                .ToList();
            Locked = @lock;
        }

        public void Update(List<ArenaInfo> arenaInfoList)
        {
            // Trust ArenaInfoList always latest data.
            foreach (var arenaInfo in arenaInfoList)
            {
                _map[arenaInfo.AvatarAddress] = arenaInfo;
            }
            OrderedArenaInfos = _map.Values
                .OrderByDescending(i => i.Score)
                .ThenBy(i => i.CombatPoint)
                .ToList();
        }

        // Copy from WeeklyArenaState.GetArenaInfos
        // https://github.com/planetarium/lib9c/blob/main/Lib9c/Model/State/WeeklyArenaState.cs#L126
        public List<(int rank, ArenaInfo arenaInfo)> GetArenaInfos(
            Address avatarAddress,
            int upperRange = 10,
            int lowerRange = 10)
        {
            var avatarRank = 0;
            for (var i = 0; i < OrderedArenaInfos.Count; i++)
            {
                var pair = OrderedArenaInfos[i];
                if (!pair.AvatarAddress.Equals(avatarAddress))
                {
                    continue;
                }

                avatarRank = i + 1;
                break;
            }

            if (avatarRank == 0)
            {
                return new List<(int rank, ArenaInfo arenaInfo)>();
            }

            var firstRank = Math.Max(1, avatarRank - upperRange);
            var lastRank = Math.Min(avatarRank + lowerRange, OrderedArenaInfos.Count);
            return GetArenaInfos(firstRank, lastRank - firstRank + 1);
        }

        // Copy from WeeklyArenaState.GetArenaInfos
        // https://github.com/planetarium/lib9c/blob/main/Lib9c/Model/State/WeeklyArenaState.cs#L94
        public List<(int rank, ArenaInfo arenaInfo)> GetArenaInfos(
            int firstRank = 1,
            int? count = null)
        {
            if (OrderedArenaInfos.Count == 0)
            {
                return new List<(int rank, ArenaInfo arenaInfo)>();
            }

            if (!(0 < firstRank && firstRank <= OrderedArenaInfos.Count))
            {
                throw new ArgumentOutOfRangeException(
                    $"{nameof(firstRank)}({firstRank}) out of range({OrderedArenaInfos.Count})");
            }

            count = count.HasValue
                ? Math.Min(OrderedArenaInfos.Count - firstRank + 1, count.Value)
                : OrderedArenaInfos.Count - firstRank + 1;

            var offsetIndex = 0;
            return OrderedArenaInfos.GetRange(firstRank - 1, count.Value)
                .Select(arenaInfo => (firstRank + offsetIndex++, arenaInfo))
                .ToList();
        }
    }
}
