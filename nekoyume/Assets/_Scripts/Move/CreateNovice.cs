namespace Nekoyume.Move
{
    [MoveName("create_novice")]
    public class CreateNovice : MoveBase
    {
        public override Context Execute(Context ctx)
        {
            return Actions.Execute(ctx);
        }
    }
}
