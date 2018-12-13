using System.Collections.Generic;
using Nekoyume.Model;
using Nekoyume.Move;

namespace Nekoyume.Action
{
    public class CreateNovice : ActionBase
    {
        private readonly string _name;

        public CreateNovice(string name)
        {
            _name = name;
        }

        public override Context Execute(Context ctx)
        {
            ctx.Avatar = new Avatar
            {
                name = _name,
                class_ = CharacterClass.Novice.ToString(),
                level = 1,
                gold = 0,
                exp = 0,
                hp = 0,
                world_stage = 1,
                CurrentHP = 0,
            };
            ctx.Status = ContextStatus.Success;
            return ctx;
        }

        public override Dictionary<string, string> ToDetails()
        {
            return new Dictionary<string, string>
            {
                ["name"] = _name
            };
        }
    }
}
