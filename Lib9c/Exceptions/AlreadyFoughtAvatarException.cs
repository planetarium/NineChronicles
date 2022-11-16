using System;

namespace Nekoyume.Exceptions
{
    [Serializable]
    public class AlreadyFoughtAvatarException : Exception
    {
        public AlreadyFoughtAvatarException()
        {
        }

        public AlreadyFoughtAvatarException(string message) : base(message)
        {
        }
    }
}
