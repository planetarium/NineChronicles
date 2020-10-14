using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidMaterialException : InvalidOperationException
    {
        public InvalidMaterialException(string s) : base(s)
        {
        }

        protected InvalidMaterialException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
