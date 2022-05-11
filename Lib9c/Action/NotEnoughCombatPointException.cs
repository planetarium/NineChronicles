using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    public class NotEnoughCombatPointException : InvalidOperationException
    {
        public NotEnoughCombatPointException(string s) : base(s)
        {
        }

        protected NotEnoughCombatPointException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }
}
