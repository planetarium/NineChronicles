namespace Nekoyume.Move
{
    [MoveName("hack_and_slash")]
    [Preprocess]
    public class HackAndSlash : MoveBase
    {
        public override Context Execute(Context ctx)
        {
            if (ctx.Avatar.dead)
            {
                throw new InvalidMoveException();
            }
            var newCtx = CreateContext(avatar: ctx.Avatar);
            newCtx.Avatar.hp = int.Parse(Details["hp"]);
            newCtx.Avatar.world_stage = int.Parse(Details["stage"]);
            newCtx.Avatar.dead = Details["dead"].ToLower() == "true";
            newCtx.Avatar.exp = int.Parse(Details["exp"]);
            newCtx.Avatar.level = int.Parse(Details["level"]);
            return newCtx;
        }
    }
}