using Nekoyume.Model;

namespace Nekoyume.Move
{
    [MoveName("create_novice")]
    public class CreateNovice : MoveBase
    {
        public override Context Execute(Context ctx)
        {
            return CreateContext(
                ContextStatus.Success,
                new Avatar
                {
                    name = Details["name"],
                    user = UserAddress,
                    gold = 0,
                    class_ = CharacterClass.Novice.ToString(),
                    level = 1,
                    world_stage = 1
                }
            );
        }
    }
}