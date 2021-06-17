using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class OrderIdDoesNotExistException : InvalidOperationException
    {
        public OrderIdDoesNotExistException(string msg) : base(msg)
        {
        }

        protected OrderIdDoesNotExistException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
