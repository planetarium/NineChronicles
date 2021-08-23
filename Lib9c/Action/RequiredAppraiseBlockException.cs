using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class RequiredAppraiseBlockException : Exception
    {
        public RequiredAppraiseBlockException(string message) : base(message)
        {
        }

        protected RequiredAppraiseBlockException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
