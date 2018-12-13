using System.Collections.Generic;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Action
{
    public class Sleep : ActionBase
    {
        private readonly Avatar _avatar;
        private readonly Stats _stats;

        public Sleep(Avatar avatar, Stats statsData)
        {
            _avatar = avatar;
            _stats = statsData;
        }

        public override Avatar Execute()
        {
            _avatar.dead = false;
            _avatar.hp = _stats.Health;
            return _avatar;
        }

        public override Dictionary<string, string> ToDetails()
        {
            return new Dictionary<string, string>
            {
                ["hp"] = _avatar.hp.ToString()
            };
        }
    }
}
