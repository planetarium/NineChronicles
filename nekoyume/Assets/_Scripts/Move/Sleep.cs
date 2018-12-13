namespace Nekoyume.Move
{
    [MoveName("sleep")]
    [Preprocess]
    public class Sleep : MoveBase
    {
        public override Context Execute(Context ctx)
        {
            var newCtx = CreateContext(avatar: ctx.Avatar);
            return Actions.Execute(newCtx);
        }
    }
}
