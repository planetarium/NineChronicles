using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class ItemDoesNotExistException : InvalidOperationException
    {
        public ItemDoesNotExistException(string msg) : base(msg)
        {
        }

        public ItemDoesNotExistException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
