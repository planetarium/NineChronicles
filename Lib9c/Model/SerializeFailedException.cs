using System;
using System.Runtime.Serialization;

namespace Nekoyume.Model
{
    [Serializable]
    public class SerializeFailedException : Exception
    {
        public SerializeFailedException(string message) : base(message)
        {
        }

        protected SerializeFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
