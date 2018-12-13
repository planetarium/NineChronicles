namespace Nekoyume.Move
{
    [MoveName("sleep")]
    [Preprocess]
    public class Sleep : MoveBase
    {
        public override Context Execute(Context ctx)
        {
            return Actions.Execute(CreateContext(avatar: ctx.Avatar));
        }
    }
}
