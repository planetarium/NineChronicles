using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidEquipmentException : Exception
    {
        public InvalidEquipmentException()
        {
        }

        public InvalidEquipmentException(string msg) : base(msg)
        {
        }

        protected InvalidEquipmentException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
