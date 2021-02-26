using Bencodex;
using Nekoyume.Model.State;
using System;
using System.Runtime.Serialization;

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

        public InvalidSignatureException(
            SerializationInfo info, StreamingContext context
        ) : base(info, context)
        {
            byte[] rawPending = (byte[])info.GetValue(nameof(Pending), typeof(byte[]));
            Pending = new PendingActivationState(
                (Bencodex.Types.Dictionary)new Codec().Decode(rawPending)
            );
            Signature = (byte[])info.GetValue(nameof(Signature), typeof(byte[]));
        }

        public override void GetObjectData(
            SerializationInfo info,
            StreamingContext context
        )
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Pending), new Codec().Encode(Pending.Serialize()));
            info.AddValue(nameof(Signature), Signature);
        }
    }
}
