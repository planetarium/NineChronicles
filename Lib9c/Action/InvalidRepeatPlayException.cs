using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidRepeatPlayException : Exception
    {
        public InvalidRepeatPlayException()
        {
        }

        public InvalidRepeatPlayException(string msg) : base(msg)
        {
        }

        protected InvalidRepeatPlayException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
