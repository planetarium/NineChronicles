using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex;
using Bencodex.Types;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Action;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class PendingActivationState : State, ISerializable
    {
        private static Address BaseAddress = Addresses.PendingActivation;

        public byte[] Nonce { get; }

        public PublicKey PublicKey { get; }

        public PendingActivationState(byte[] nonce, PublicKey publicKey)
            : base (DeriveAddress(nonce, publicKey))
        {
            Nonce = nonce;
            PublicKey = publicKey;
        }

        private static Address DeriveAddress(byte[] nonce, PublicKey publicKey)
        {
            return BaseAddress.Derive(nonce.Concat(publicKey.Format(true)).ToArray());
        }

        public PendingActivationState(Dictionary serialized) 
            : base(serialized)
        {
            Nonce = (Binary)serialized["nonce"];
            PublicKey = serialized["public_key"].ToPublicKey();
        }

        protected PendingActivationState(SerializationInfo info, StreamingContext context)
            : this((Dictionary) new Codec().Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        public override IValue Serialize()
        {
            var values = new Dictionary<IKey, IValue>
            {
                [(Text) "nonce"] = (Binary) Nonce,
                [(Text) "public_key"] = PublicKey.Serialize(),
            };
            
            return new Dictionary(values.Union((Dictionary)base.Serialize()));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("serialized", new Codec().Encode(Serialize()));
        }
    }
}
