using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class DuplicateEquipmentException: InvalidOperationException
    {
        public DuplicateEquipmentException(string s) : base(s)
        {
        }

        protected DuplicateEquipmentException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
