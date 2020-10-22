using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class CostumeSlotUnlockException : InvalidOperationException
    {
        public CostumeSlotUnlockException(string s) : base(s)
        {
        }

        protected CostumeSlotUnlockException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
