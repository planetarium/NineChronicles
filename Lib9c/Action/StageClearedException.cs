using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    public class StageClearedException : Exception
    {
        public StageClearedException()
        {
        }

        public StageClearedException(string msg) : base(msg)
        {
        }

        protected StageClearedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
