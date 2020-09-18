using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class NotEnoughClearedStageLevelException : Exception
    {
        public NotEnoughClearedStageLevelException(string message) : base(message)
        {
        }

        public NotEnoughClearedStageLevelException(int require, int current) : this(
            $"Aborted as the signer is not cleared the minimum stage level required: {current} < {require}.")
        {
        }

        public NotEnoughClearedStageLevelException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
