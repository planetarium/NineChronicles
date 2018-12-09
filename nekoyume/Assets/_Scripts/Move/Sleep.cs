namespace Nekoyume.Move
{
    [MoveName("sleep")]
    [Preprocess]
    public class Sleep : MoveBase
    {
        public override Context Execute(Context ctx)
        {
            var newCtx = CreateContext(avatar: ctx.Avatar);
            var avatar = Actions.Execute();
            newCtx.Avatar = avatar;
            return newCtx;
        }
    }
}
