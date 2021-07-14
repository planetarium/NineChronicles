using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidTradableIdException : InvalidOperationException
    {
        protected InvalidTradableIdException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public InvalidTradableIdException(string message) : base(message)
        {
        }
    }
}
