using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class DeletedAvatarState : AvatarState
    {
        public long deletedAt;

        public DeletedAvatarState(AvatarState avatarState, long blockIndex)
            : base(avatarState)
        {
            deletedAt = blockIndex;
        }

        public DeletedAvatarState(Dictionary serialized)
            : base(serialized)
        {
            deletedAt = serialized["deletedAt"].ToLong();
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "deletedAt"] = deletedAt.Serialize(),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
    }
}
