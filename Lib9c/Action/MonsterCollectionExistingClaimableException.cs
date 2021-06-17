using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class MonsterCollectionExistingClaimableException : Exception
    {
        public MonsterCollectionExistingClaimableException()
        {
        }

        public MonsterCollectionExistingClaimableException(string message) : base(message)
        {
        }

        public MonsterCollectionExistingClaimableException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MonsterCollectionExistingClaimableException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
