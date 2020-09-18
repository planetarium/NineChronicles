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

        public WeeklyArenaStateNotContainsAvatarAddressException(Address avatarAddress)
            : this($"Aborted as the weekly arena state not contains {avatarAddress}.")
        {
        }

        public WeeklyArenaStateNotContainsAvatarAddressException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
