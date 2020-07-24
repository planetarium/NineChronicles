using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class FailedLoadStateException : Exception
    {
        public FailedLoadStateException(string message) : base(message)
        {
        }

        protected FailedLoadStateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
