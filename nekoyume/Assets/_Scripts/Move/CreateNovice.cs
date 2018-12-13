namespace Nekoyume.Move
{
    [MoveName("create_novice")]
    public class CreateNovice : MoveBase
    {
        public override Context Execute(Context ctx)
        {
            var avatar = Actions.Execute();
            return CreateContext(
                ContextStatus.Success,
                avatar
            );
        }
    }
}
