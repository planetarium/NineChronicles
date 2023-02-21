using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidCurrencyException : InvalidOperationException
    {
        public InvalidCurrencyException(string msg) : base(msg)
        {
        }

        protected InvalidCurrencyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
