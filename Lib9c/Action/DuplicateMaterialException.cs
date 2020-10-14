using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class DuplicateMaterialException : InvalidOperationException
    {
        public DuplicateMaterialException(string s) : base(s)
        {
        }

        protected DuplicateMaterialException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
