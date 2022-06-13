using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    /// <summary>
    /// An exception thrown when there is unexpected monster collection.
    /// </summary>
    [Serializable]
    public class MonsterCollectionExistingException : Exception
    {
        public MonsterCollectionExistingException()
        {
        }

        public MonsterCollectionExistingException(string message) : base(message)
        {
        }

        public MonsterCollectionExistingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MonsterCollectionExistingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
