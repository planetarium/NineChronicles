using System;
using System.Runtime.Serialization;

namespace Nekoyume.Model.Rune
{
    [Serializable]
    public class RuneNotFoundException : Exception
    {
        public RuneNotFoundException(string message) : base(message)
        {
        }

        protected RuneNotFoundException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class RuneCostNotFoundException : Exception
    {
        public RuneCostNotFoundException(string message) : base(message)
        {
        }

        protected RuneCostNotFoundException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class RuneCostDataNotFoundException : Exception
    {
        public RuneCostDataNotFoundException(string message) : base(message)
        {
        }

        protected RuneCostDataNotFoundException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

}
