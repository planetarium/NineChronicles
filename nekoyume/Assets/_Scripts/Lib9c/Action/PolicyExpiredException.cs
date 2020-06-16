using Nekoyume.Model.State;
using System;

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
    }
}
