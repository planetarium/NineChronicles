using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidLevelException : InvalidOperationException
    {
        public InvalidLevelException(string s) : base(s)
        {
        }

        protected InvalidLevelException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
