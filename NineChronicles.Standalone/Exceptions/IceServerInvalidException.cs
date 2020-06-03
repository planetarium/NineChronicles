using System;

namespace NineChronicles.Standalone.Exceptions
{
    public class IceServerInvalidException: Exception
    {
        public IceServerInvalidException()
        {
        }

        public IceServerInvalidException(string message)
            : base(message)
        {
        }

        public IceServerInvalidException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
