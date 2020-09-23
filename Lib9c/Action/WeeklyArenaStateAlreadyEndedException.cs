using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class WeeklyArenaStateAlreadyEndedException : Exception
    {
        public WeeklyArenaStateAlreadyEndedException(string message) : base(message)
        {
        }

        public WeeklyArenaStateAlreadyEndedException()
            : this("Aborted as the weekly arena state already ended.")
        {
        }

        public WeeklyArenaStateAlreadyEndedException(
            SerializationInfo info,
            StreamingContext context) : base (info, context)
        {
        }
    }
}
