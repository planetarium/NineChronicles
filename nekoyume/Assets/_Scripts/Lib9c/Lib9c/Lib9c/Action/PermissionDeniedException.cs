using Libplanet;
using Nekoyume.Model.State;
using System;
using System.Runtime.Serialization;
using Libplanet.Serialization;

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


        public PermissionDeniedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Signer = info.GetValue<Address>(nameof(Signer));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(nameof(Signer), Signer);
        }
    }
}
