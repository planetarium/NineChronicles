using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class NotEnoughClearedStageLevelException : Exception
    {
        public NotEnoughClearedStageLevelException(string message)
            : base(message)
        {
        }

        public NotEnoughClearedStageLevelException(
            string addressesHex,
            int require,
            int current)
            : this(
                $"{addressesHex}Aborted as the signer" +
                " is not cleared the minimum stage level required:" +
                $" {current} < {require}.")
        {
        }
        
        public NotEnoughClearedStageLevelException(
            string actionType,
            string addressesHex,
            int require,
            int current)
            : this(
                $"[{actionType}][{addressesHex}] Aborted as the signer" +
                " is not cleared the minimum stage level required:" +
                $" {current} < {require}.")
        {
        }

        public NotEnoughClearedStageLevelException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
