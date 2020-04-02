using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class DeletedAvatarState : AvatarState
    {
        public DateTimeOffset deletedAt;

        public DeletedAvatarState(AvatarState avatarState, DateTimeOffset deletedAt)
            : base(avatarState)
        {
            this.deletedAt = deletedAt;
        }

        public DeletedAvatarState(Dictionary serialized)
            : base(serialized)
        {
            deletedAt = serialized["deletedAt"].ToDateTimeOffset();
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "deletedAt"] = deletedAt.Serialize(),
            }.Union((Dictionary) base.Serialize()));
    }
}
