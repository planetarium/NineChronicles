using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidActionFieldException : Exception
    {
        public InvalidActionFieldException()
        {
        }

        public InvalidActionFieldException(string message)
            : base(message)
        {
        }

        public InvalidActionFieldException(
            string message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        public InvalidActionFieldException(
            string actionType,
            string fieldName,
            string addressesHex,
            string message = "")
            : base($"[{actionType}] {fieldName} [{addressesHex}]: {message}")
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
