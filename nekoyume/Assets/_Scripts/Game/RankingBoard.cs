using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Model;

namespace Nekoyume.Game
{
    [Serializable]
    public class RankingBoard
    {
        private readonly HashSet<Context> _map;

        public RankingBoard()
        {
            _map = new HashSet<Context>();
        }
        public void Update(Context ctx)
        {
            var current = _map.FirstOrDefault(c => c.AvatarAddress == ctx.AvatarAddress);
            if (!ReferenceEquals(current, null))
            {
                if (current.avatar.WorldStage < ctx.avatar.WorldStage)
                {
                    _map.Remove(current);
                }
                else
                {
                    return;
                }
            }

            _map.Add(ctx);
        }

        public Avatar[] GetAvatars(DateTimeOffset? dt)
        {
            IEnumerable<Context> map =
                _map.OrderByDescending(c => c.avatar.WorldStage).ThenBy(c => c.clearedAt);
            if (dt != null)
            {
                map = map.Where(context => ((TimeSpan) (dt - context.updatedAt)).Days <= 1);
            }

            return map.Select(c => c.avatar).ToArray();
        }
    }
}
