using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    public class NotEnoughGatheredStarException : Exception
    {
        public NotEnoughGatheredStarException(string s) : base(s)
        {
        }

        protected NotEnoughGatheredStarException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }
}
