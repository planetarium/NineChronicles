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
                    class_ = CharacterClass.Novice.ToString(),
                    level = 1,
                    gold = 0,
                    exp = 0,
                    hp = 0,
                    world_stage = 1,
                    user = UserAddress
                }
            );
        }
    }
}
