using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class PlayCountIsZeroException : Exception
    {
        public PlayCountIsZeroException(string msg) : base(msg)
        {
        }

        public PlayCountIsZeroException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
