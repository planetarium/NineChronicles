namespace Nekoyume.Move
{
    [MoveName("move_zone")]
    [Preprocess]
    public class MoveZone : MoveBase
    {
        public override Context Execute(Context ctx)
        {
            var newCtx = CreateContext(avatar: ctx.Avatar);
            newCtx.Avatar.WorldStage = int.Parse(Details["zone"]);
            return newCtx;
        }
    }
}
