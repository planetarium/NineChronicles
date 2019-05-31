using System;
using Libplanet;

namespace Nekoyume.State
{
    [Serializable]
    public class DeletedAvatarState : AvatarState
    {
        public Address agentAddress;
        public DateTimeOffset deletedAt;
        
        public DeletedAvatarState(AvatarState avatarState, Address agentAddress, DateTimeOffset deletedAt) : base(avatarState)
        {
            this.agentAddress = agentAddress;
            this.deletedAt = deletedAt;
        }
    }
}
