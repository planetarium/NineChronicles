using System;
using System.Runtime.Serialization;
using Libplanet;

namespace Nekoyume.Action
{
    [Serializable]
    public class WeeklyArenaStateNotContainsAvatarAddressException : Exception
    {
        public WeeklyArenaStateNotContainsAvatarAddressException(string message)
            : base(message)
        {
        }

        public WeeklyArenaStateNotContainsAvatarAddressException(string addressesHex, Address avatarAddress)
            : this($"{addressesHex}Aborted as the weekly arena state not contains {avatarAddress.ToHex()}.")
        {
        }

        public WeeklyArenaStateNotContainsAvatarAddressException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
