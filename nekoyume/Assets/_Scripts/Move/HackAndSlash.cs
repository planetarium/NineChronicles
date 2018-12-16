namespace Nekoyume.Move
{
    [MoveName("hack_and_slash")]
    [Preprocess]
    public class HackAndSlash : MoveBase
    {
        public override Context Execute(Context ctx)
        {
            if (ctx.Avatar.Dead)
            {
                throw new InvalidMoveException();
            }
            var newCtx = CreateContext(avatar: ctx.Avatar);
            newCtx.Avatar.CurrentHP = int.Parse(Details["hp"]);
            newCtx.Avatar.WorldStage = int.Parse(Details["stage"]);
            newCtx.Avatar.Dead = Details["dead"].ToLower() == "true";
            newCtx.Avatar.EXP = int.Parse(Details["exp"]);
            newCtx.Avatar.Level = int.Parse(Details["level"]);
            string items;
            Details.TryGetValue("items", out items);
            if (!string.IsNullOrEmpty(items))
            {
                newCtx.Avatar.Items = items;
            }
            return newCtx;
        }
    }
}
