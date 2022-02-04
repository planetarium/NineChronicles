using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class HighLevelItemRequirementException : Exception
    {
        public HighLevelItemRequirementException(string message) : base(message)
        {
        }

        public HighLevelItemRequirementException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
