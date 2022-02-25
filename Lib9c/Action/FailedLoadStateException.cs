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
        
        public FailedLoadStateException(Type stateType, Address address) :
            base($"state type: {stateType}, address: {address.ToHex()}")
        {
        }

        protected FailedLoadStateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
