using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class AlreadyContractedException : InvalidOperationException
    {
        public AlreadyContractedException(string s) : base(s)
        {
        }

        protected AlreadyContractedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
