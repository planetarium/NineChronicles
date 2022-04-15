using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class AlreadyWorldUnlockedException: Exception
    {
        public AlreadyWorldUnlockedException()
        {
        }

        public AlreadyWorldUnlockedException(string msg) : base(msg)
        {
        }

        protected AlreadyWorldUnlockedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

    }
}
