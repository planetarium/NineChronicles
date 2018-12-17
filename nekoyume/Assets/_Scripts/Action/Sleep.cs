using System.Collections.Generic;
using Nekoyume.Move;

namespace Nekoyume.Action
{
    public class Sleep : ActionBase
    {
        public override Context Execute(Context ctx)
        {
            ctx.Avatar.Dead = false;
            ctx.Avatar.CurrentHP = ctx.Avatar.HPMax;
            return ctx;
        }

        public override Dictionary<string, string> ToDetails()
        {
            return new Dictionary<string, string>();
        }
    }
}
