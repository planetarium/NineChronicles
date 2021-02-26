using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class ArenaNotEndedException : InvalidOperationException
    {
        public ArenaNotEndedException(string s) : base(s)
        {
        }

        protected ArenaNotEndedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
