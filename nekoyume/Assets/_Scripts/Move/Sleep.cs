namespace Nekoyume.Move
{
    [MoveName("sleep")]
    [Preprocess]
    public class Sleep : MoveBase
    {
        public override Context Execute(Context ctx)
        {
            var newCtx = CreateContext(avatar: ctx.Avatar);
            foreach (var action in Actions)
            {
                newCtx = action.Execute(newCtx);
            }

            return newCtx;
        }
    }
}
