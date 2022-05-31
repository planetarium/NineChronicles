using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    public class StageNotClearedException : Exception
    {
        public StageNotClearedException()
        {
        }

        public StageNotClearedException(string msg) : base(msg)
        {
        }

        protected StageNotClearedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
