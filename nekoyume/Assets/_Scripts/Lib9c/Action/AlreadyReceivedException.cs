using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class AlreadyReceivedException : InvalidOperationException
    {
        public AlreadyReceivedException(string s) : base(s)
        {
        }

        protected AlreadyReceivedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
