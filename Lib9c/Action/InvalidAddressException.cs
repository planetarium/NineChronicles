using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    public class InvalidAddressException : InvalidOperationException
    {
        public InvalidAddressException()
        {
        }

        protected InvalidAddressException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
