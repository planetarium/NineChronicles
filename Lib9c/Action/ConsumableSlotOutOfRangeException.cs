using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class ConsumableSlotOutOfRangeException : Exception
    {
        public ConsumableSlotOutOfRangeException() : base()
        {
        }

        public ConsumableSlotOutOfRangeException(string message) : base(message)
        {
        }

        protected ConsumableSlotOutOfRangeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

    }
}
