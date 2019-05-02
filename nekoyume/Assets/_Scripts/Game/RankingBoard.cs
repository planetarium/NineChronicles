using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;

namespace Nekoyume.Game
{
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
    }
}
