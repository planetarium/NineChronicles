namespace Nekoyume.Move
{
    [MoveName("sleep")]
    [Preprocess]
    public class Sleep : MoveBase
    {
        public override Context Execute(Context ctx)
        {
            var newCtx = CreateContext(avatar: ctx.Avatar);
            newCtx.Avatar.dead = false;
            string data;
            int hp;
            Details.TryGetValue("hp", out data);
            int.TryParse(data, out hp);
            newCtx.Avatar.hp = hp;
            return newCtx;
        }
    }
}