using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Libplanet;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidTransferMinterException : Exception
    {
        public HashSet<Address> Minters { get; }

        public Address Sender { get; }

        public Address Recipient { get; }

        public InvalidTransferMinterException(IEnumerable<Address> minters, Address sender, Address recipient)
        {
            Minters = new HashSet<Address>(minters);
            Sender = sender;
            Recipient = recipient;
        }

        protected InvalidTransferMinterException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
