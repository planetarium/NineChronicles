using Libplanet;
using Libplanet.Tx;
using System;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidTransferSignerException : Exception
    {
        public InvalidTransferSignerException(
            Address txSigner,
            Address sender,
            Address recipient)
        {
            TxSigner = txSigner;
            Sender = sender;
            Recipient = recipient;
        }

        public Address TxSigner { get; }

        public Address Sender { get; }

        public Address Recipient { get; }
    }
}
