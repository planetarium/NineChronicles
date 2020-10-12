using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidNamePatternException : InvalidOperationException
    {
        public InvalidNamePatternException(string s) : base(s)
        {
        }

        protected InvalidNamePatternException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
