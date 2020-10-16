using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class CombinationSlotResultNullException : Exception
    {
        public CombinationSlotResultNullException(string message) : base (message)
        {
        }

        public CombinationSlotResultNullException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
