using System;

namespace Lib9c.Exceptions
{
    [Serializable]
    public class InvalidStateTypeException : Exception
    {
        public InvalidStateTypeException(string message)
            : base(message)
        {
        }
    }
}
