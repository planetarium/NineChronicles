using Nekoyume.Model.State;
using System;
using System.Runtime.Serialization;
using Libplanet.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class PolicyExpiredException : AdminPermissionException
    {
        public long BlockIndex { get; }

        public PolicyExpiredException(AdminState policy, long blockIndex)
            : base(policy)
        {
            BlockIndex = blockIndex;
        }

        public PolicyExpiredException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            BlockIndex = info.GetValue<long>(nameof(BlockIndex));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(nameof(BlockIndex), BlockIndex);
        }
    }
}
