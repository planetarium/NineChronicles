using Libplanet;
using System;

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
    }
}
