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
            var current = _map.FirstOrDefault(c => c.address == state.address);
            if (!ReferenceEquals(current, null))
            {
                if (current.worldStage < state.worldStage)
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

        public AvatarState[] GetAvatars(DateTimeOffset? dt)
        {
            IEnumerable<AvatarState> map =
                _map.OrderByDescending(c => c.worldStage).ThenBy(c => c.clearedAt);
            if (dt != null)
            {
                map = map.Where(context => ((TimeSpan) (dt - context.updatedAt)).Days <= 1);
            }

            return map.ToArray();
        }
    }
}
