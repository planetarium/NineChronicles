using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class AvatarIndexAlreadyUsedException : InvalidOperationException
    {
        public AvatarIndexAlreadyUsedException(string s) : base(s)
        {
        }

        protected AvatarIndexAlreadyUsedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
