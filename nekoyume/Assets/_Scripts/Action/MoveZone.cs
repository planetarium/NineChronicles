using System.Collections.Generic;
using Nekoyume.Move;

namespace Nekoyume.Action
{
    public class MoveZone : ActionBase
    {
        private readonly int _stage;

        public MoveZone(int stage)
        {
            _stage = stage;
        }

        public override Context Execute(Context ctx)
        {
            ctx.Avatar.WorldStage = _stage;
            return ctx;
        }

        public override Dictionary<string, string> ToDetails()
        {
            return new Dictionary<string, string>
            {
                ["zone"] = _stage.ToString()
            };
        }
    }
}
