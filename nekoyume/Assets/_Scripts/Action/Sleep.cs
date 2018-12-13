using System.Collections.Generic;
using Nekoyume.Move;

namespace Nekoyume.Action
{
    public class Sleep : ActionBase
    {
        public override Context Execute(Context ctx)
        {
            ctx.Avatar.dead = false;
            ctx.Avatar.CurrentHP = ctx.Avatar.hp;
            return ctx;
        }

        public override Dictionary<string, string> ToDetails()
        {
            return new Dictionary<string, string>();
        }
    }
}
