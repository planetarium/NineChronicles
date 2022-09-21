using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    public class NotEnoughHammerPointException : Exception
    {
        public NotEnoughHammerPointException(string s) : base(s)
        {
        }

        protected NotEnoughHammerPointException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }
}
