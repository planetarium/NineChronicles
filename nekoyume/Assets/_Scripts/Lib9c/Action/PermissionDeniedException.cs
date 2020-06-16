using Libplanet;
using Nekoyume.Model.State;
using System;

namespace Nekoyume.Action
{
    [Serializable]
    public class PermissionDeniedException : AdminPermissionException
    {
        public Address Signer { get; }

        public PermissionDeniedException(AdminState policy, Address signer)
            : base(policy)
        {
            Signer = signer;
        }
    }
}
