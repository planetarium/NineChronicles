using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class NotEnoughWeeklyArenaChallengeCountException : Exception
    {
        public NotEnoughWeeklyArenaChallengeCountException(string message) : base(message)
        {
        }

        public NotEnoughWeeklyArenaChallengeCountException()
            : this("Aborted as the arena state reached the daily limit.")
        {
        }

        public NotEnoughWeeklyArenaChallengeCountException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
