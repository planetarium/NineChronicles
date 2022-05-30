using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    public class NotEnoughStarException : Exception
    {
        public NotEnoughStarException(string s) : base(s)
        {
        }

        protected NotEnoughStarException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }
}
