using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class StakeExistingClaimableException : Exception
    {
        public StakeExistingClaimableException()
        {
        }

        public StakeExistingClaimableException(string message) : base(message)
        {
        }

        public StakeExistingClaimableException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected StakeExistingClaimableException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
