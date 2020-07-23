using System;

namespace Nekoyume.Action
{
    [Serializable]
    public class FailedLoadStateException : Exception
    {
        public FailedLoadStateException(string message) : base(message)
        {
        }
    }
}
