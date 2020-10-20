using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class DuplicateCostumeException: InvalidOperationException
    {
        public DuplicateCostumeException(string s) : base(s)
        {
        }

        protected DuplicateCostumeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
