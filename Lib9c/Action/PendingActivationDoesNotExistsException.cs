using Libplanet;
using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class PendingActivationDoesNotExistsException : ActivationException
    {
        public Address PendingAddress { get; }

        public PendingActivationDoesNotExistsException(Address pendingAddress)
        {
            PendingAddress = pendingAddress;
        }

        public PendingActivationDoesNotExistsException(
            SerializationInfo info, StreamingContext context
        ) : base(info, context)
        {
            PendingAddress = (Address)info.GetValue(
                nameof(PendingAddress), 
                typeof(Address)
            );
        }

        public override void GetObjectData(
            SerializationInfo info, 
            StreamingContext context
        )
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(PendingAddress), PendingAddress);
        }
    }
}
