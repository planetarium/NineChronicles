using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidStakingRoundException : Exception
    {
        public InvalidStakingRoundException(string msg) : base(msg)
        {
        }

        protected InvalidStakingRoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
