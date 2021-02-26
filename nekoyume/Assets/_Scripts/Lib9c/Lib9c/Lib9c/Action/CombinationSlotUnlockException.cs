using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class CombinationSlotUnlockException : InvalidOperationException
    {
        public CombinationSlotUnlockException(string s) : base(s)
        {
        }

        protected CombinationSlotUnlockException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
