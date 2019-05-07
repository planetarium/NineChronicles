using System;
using Libplanet;
using Nekoyume.Model;

namespace Nekoyume.Action
{
    [Serializable]
    public class Context
    {
        public Avatar avatar;
        public BattleLog battleLog;
        public int gold;
        public DateTimeOffset updatedAt;
        public DateTimeOffset? clearedAt;
        public Address? AvatarAddress { get; private set; }

        public Context(Avatar avatar, Address? address, BattleLog logs = null, int gold = 0)
        {
            this.avatar = avatar;
            battleLog = logs;
            this.gold = gold;
            updatedAt = DateTimeOffset.UtcNow;
            AvatarAddress = address;
        }
    }
}
