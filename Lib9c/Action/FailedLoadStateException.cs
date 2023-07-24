using System;
using System.Runtime.Serialization;
using Libplanet.Crypto;

namespace Nekoyume.Action
{
    [Serializable]
    public class FailedLoadStateException : Exception
    {
        public FailedLoadStateException(string message) : base(message)
        {
        }

        public FailedLoadStateException(string message, Exception innerException = null) :
            base(message, innerException)
        {
        }

        public FailedLoadStateException(
            Address address,
            Type stateType,
            Exception innerException = null) :
            base(
                $"address: {address.ToHex()}, state type: {stateType.FullName}",
                innerException)
        {
        }

        public FailedLoadStateException(
            string actionType,
            string addressesHex,
            Type stateType,
            Address address) :
            this(
                actionType,
                addressesHex,
                $"type({stateType.FullName}) at address({address.ToHex()})")
        {
        }

        public FailedLoadStateException(
            string actionType,
            string addressesHex,
            string message,
            Exception innerException = null)
            : base(
                $"[{actionType}][{addressesHex}] {message}",
                innerException)
        {
        }

        protected FailedLoadStateException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
