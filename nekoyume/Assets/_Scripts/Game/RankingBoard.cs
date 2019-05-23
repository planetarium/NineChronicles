using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model;
using Nekoyume.State;

namespace Nekoyume.Game
{
    [Serializable]
    public class RankingBoard
    {
        private readonly HashSet<AvatarState> _map;

        public RankingBoard()
        {
            _map = new HashSet<AvatarState>();
        }
        public void Update(AvatarState state)
        {
            var current = _map.FirstOrDefault(c => c.AvatarAddress == state.AvatarAddress);
            if (!ReferenceEquals(current, null))
            {
                if (current.avatar.WorldStage < state.avatar.WorldStage)
                {
                    _map.Remove(current);
                }
                else
                {
                    return;
                }
            }

            _map.Add(state);
        }

        public Avatar[] GetAvatars(DateTimeOffset? dt)
        {
            IEnumerable<AvatarState> map =
                _map.OrderByDescending(c => c.avatar.WorldStage).ThenBy(c => c.clearedAt);
            if (dt != null)
            {
                map = map.Where(context => ((TimeSpan) (dt - context.updatedAt)).Days <= 1);
            }

            return map.Select(c => c.avatar).ToArray();
        }
    }
}
