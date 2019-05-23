using System;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model;

namespace Nekoyume.State
{
    /// <summary>
    /// Agent가 포함하는 각 Avatar의 상태 모델이다.
    /// </summary>
    [Serializable]
    public class AvatarState
    {
        public Avatar avatar;
        public BattleLog battleLog;
        public DateTimeOffset updatedAt;
        public DateTimeOffset? clearedAt;
        public Address? AvatarAddress { get; }

        public AvatarState(Avatar avatar, Address? address, BattleLog logs = null)
        {
            this.avatar = avatar;
            battleLog = logs;
            updatedAt = DateTimeOffset.UtcNow;
            AvatarAddress = address;
        }
    }
}
