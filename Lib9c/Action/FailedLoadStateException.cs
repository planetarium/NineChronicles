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
            Type stateType,
            Address address,
            string addressesHex)
            : base($"[{actionType}] type({stateType.FullName}) at address({address.ToHex()}) [{addressesHex}]")
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
