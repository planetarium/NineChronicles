using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class MemoLengthOverflowException : InvalidOperationException
    {
        public MemoLengthOverflowException(string message) : base(message)
        {
        }

        protected MemoLengthOverflowException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
