using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class RequiredBlockIndexException : Exception
    {
        public RequiredBlockIndexException()
        {
        }

        public RequiredBlockIndexException(string msg) : base(msg)
        {
        }

        protected RequiredBlockIndexException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
