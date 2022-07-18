using System;
using System.Runtime.Serialization;

namespace Nekoyume.Exceptions
{
    [Serializable]
    public class NotEnoughEventDungeonTicketsException : Exception
    {
        public NotEnoughEventDungeonTicketsException()
        {
        }

        public NotEnoughEventDungeonTicketsException(string message)
            : base(message)
        {
        }

        public NotEnoughEventDungeonTicketsException(
            string message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        public NotEnoughEventDungeonTicketsException(
            string actionType,
            string addressesHex,
            int requiredAmount,
            int currentAmount)
            : base($"[{actionType}] required({requiredAmount}), current({currentAmount}) [{addressesHex}]")
        {
        }

        protected NotEnoughEventDungeonTicketsException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
