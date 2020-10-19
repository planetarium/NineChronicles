using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class ConsumableSlotUnlockException : InvalidOperationException
    {
        public ConsumableSlotUnlockException(string s) : base(s)
        {
        }

        protected ConsumableSlotUnlockException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
