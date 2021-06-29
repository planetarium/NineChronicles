using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class ActionPointExceededException : InvalidOperationException
    {
        public ActionPointExceededException()
        {
        }

        public ActionPointExceededException(string s) : base(s)
        {
        }

        protected ActionPointExceededException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
