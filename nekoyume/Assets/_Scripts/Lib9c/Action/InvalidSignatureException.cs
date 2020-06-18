using Nekoyume.Model.State;
using System;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidSignatureException : ActivationException
    {
        public PendingActivationState Pending { get; }
        public byte[] Signature { get; }

        public InvalidSignatureException(PendingActivationState pending, byte[] signature)
        {
            Pending = pending;
            Signature = signature;
        }
    }
}
