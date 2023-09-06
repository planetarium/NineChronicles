using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action.Exceptions
{
    public class InvalidSummonCountException : ArgumentException
    {
        public InvalidSummonCountException()
        {
        }

        public InvalidSummonCountException(string msg) : base(msg)
        {
        }

        protected InvalidSummonCountException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }
}
