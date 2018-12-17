using System;

namespace Nekoyume.Move
{
    [MoveName("move_stage")]
    [Preprocess]
    public class MoveStage : MoveBase
    {
        public override Context Execute(Context ctx)
        {
            var newCtx = CreateContext(avatar: ctx.Avatar);
            newCtx.Avatar.WorldStage = int.Parse(Details["stage"]);
            return newCtx;
        }
    }
}
