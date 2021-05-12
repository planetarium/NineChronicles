using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidMonsterCollectionRoundException : InvalidOperationException
    {
        public InvalidMonsterCollectionRoundException(string msg) : base(msg)
        {
        }

        protected InvalidMonsterCollectionRoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
