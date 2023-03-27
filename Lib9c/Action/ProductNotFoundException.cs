using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class ProductNotFoundException : InvalidOperationException
    {
        public ProductNotFoundException(string msg) : base(msg)
        {
        }

        public ProductNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
