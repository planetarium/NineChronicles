using System;
using System.Runtime.Serialization;

namespace Nekoyume.Exceptions
{
    [Serializable]
    public class InvalidActionFieldException : Exception
    {
        public InvalidActionFieldException(string message) : base(message)
        {
        }

        public InvalidActionFieldException(
            string message,
            Exception innerException = null)
            : base(message, innerException)
        {
        }

        public InvalidActionFieldException(
            string actionType,
            string addressesHex,
            string fieldName,
            string message,
            Exception innerException = null)
            : base(
                $"[{actionType}][{addressesHex}]" +
                $" Invalid field({fieldName}): {message}",
                innerException)
        {
        }

        protected InvalidActionFieldException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
