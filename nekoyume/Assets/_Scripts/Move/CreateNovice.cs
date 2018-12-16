namespace Nekoyume.Move
{
    [MoveName("create_novice")]
    public class CreateNovice : MoveBase
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
