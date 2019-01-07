using System.Collections.Generic;

namespace Nekoyume.Action
{
    public class MoveZone : ActionBase
    {
        private readonly int _stage;

        public MoveZone(int stage)
        {
            _stage = stage;
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
