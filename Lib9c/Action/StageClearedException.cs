using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class StageNotClearedException : Exception
    {
        public StageNotClearedException()
        {
        }

        public StageNotClearedException(string message)
            : base(message)
        {
        }

        public StageNotClearedException(
            string message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        public StageNotClearedException(
            string actionType,
            string addressesHex,
            int requiredToClearedStage,
            int currentClearedStage)
            : base(
                $"[{actionType}] [{addressesHex}]: required({requiredToClearedStage}), current({currentClearedStage})")
        {
        }

        protected StageNotClearedException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
