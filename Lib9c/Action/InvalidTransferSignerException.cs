using Libplanet;
using Libplanet.Tx;
using System;
using System.Runtime.Serialization;

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
        
        public InvalidTransferSignerException(
            SerializationInfo info, StreamingContext context
        ) : base(info, context)
        {
            TxSigner = (Address)info.GetValue(nameof(TxSigner), typeof(Address));
            Sender = (Address)info.GetValue(nameof(Sender), typeof(Address));
            Recipient = (Address)info.GetValue(nameof(Recipient), typeof(Address));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(TxSigner), TxSigner);
            info.AddValue(nameof(Sender), Sender);
            info.AddValue(nameof(Recipient), Recipient);
        }

        public Address TxSigner { get; }

        public Address Sender { get; }

        public Address Recipient { get; }
    }
}
