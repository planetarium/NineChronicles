using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class AlreadyActivatedException : InvalidOperationException
    {
        public AlreadyActivatedException(string s) : base(s)
        {
        }

        protected AlreadyActivatedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
