using System;
using Libplanet;

namespace Nekoyume.State
{
    [Serializable]
    public class DeletedAvatarState : AvatarState
    {
        public DateTimeOffset deletedAt;
        
        public DeletedAvatarState(AvatarState avatarState, DateTimeOffset deletedAt) : base(avatarState)
        {
            this.deletedAt = deletedAt;
        }
    }
}
