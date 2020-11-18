using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class AgentStateNotContainsAvatarAddressException : Exception
    {
        public AgentStateNotContainsAvatarAddressException(string message) : base(message)
        {
        }

        public AgentStateNotContainsAvatarAddressException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
