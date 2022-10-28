using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class ActionUnAvailableException : InvalidOperationException
    {
        public ActionUnAvailableException()
        {
        }

        public ActionUnAvailableException(string s) : base(s)
        {
        }

        protected ActionUnAvailableException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
