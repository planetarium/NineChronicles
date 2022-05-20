using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    public class InvalidGachaCountException : Exception
    {
        public InvalidGachaCountException(string s) : base(s)
        {
        }

        protected InvalidGachaCountException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }
}
