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
                Name = _name,
                Level = 1,
                EXP = 0,
                HPMax = 0,
                WorldStage = 1,
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
