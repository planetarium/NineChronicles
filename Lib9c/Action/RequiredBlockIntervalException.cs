using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class RequiredBlockIntervalException : RequiredBlockIndexException
    {
        public RequiredBlockIntervalException()
        {
        }

        public RequiredBlockIntervalException(string msg) : base(msg)
        {
        }

        public RequiredBlockIntervalException(
            string actionType,
            string addressesHex,
            long currentBlockIndex)
            : base(actionType, addressesHex, currentBlockIndex)
        {
        }

        public RequiredBlockIntervalException(
            string actionType,
            string addressesHex,
            long requiredBlockIndex,
            long currentBlockIndex)
            : base(actionType, addressesHex, requiredBlockIndex, currentBlockIndex)
        {
        }

        protected RequiredBlockIntervalException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
