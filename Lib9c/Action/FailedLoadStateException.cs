using System;
using System.Runtime.Serialization;
using Libplanet;

namespace Nekoyume.Action
{
    [Serializable]
    public class FailedLoadStateException : Exception
    {
        public FailedLoadStateException(string message) : base(message)
        {
        }
        
        public FailedLoadStateException(Address address, Type stateType) :
            base($"address: {address.ToHex()}, state type: {stateType.FullName}")
        {
        }
        
        public FailedLoadStateException(
            string actionType,
            string addressesHex,
            Type stateType,
            Address address)
            : base($"[{actionType}][{addressesHex}] type({stateType.FullName}) at address({address.ToHex()})")
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
