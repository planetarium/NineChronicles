using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidClaimException : InvalidOperationException
    {
        public InvalidClaimException()
        {
        }

        public InvalidClaimException(string msg) : base(msg)
        {
        }

        protected InvalidClaimException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
