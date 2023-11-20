#nullable enable

using System;
using System.Runtime.Serialization;
using Libplanet.Crypto;
using Libplanet.Common.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidMinterException : Exception
    {
        private Address _signer;

        public InvalidMinterException()
        {
        }


        public InvalidMinterException(Address signer)
        {
            _signer = signer;
        }

        protected InvalidMinterException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _signer = new Address(info.GetValue<byte[]>(nameof(_signer)));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(nameof(_signer), _signer.ToByteArray());
        }
    }
}
