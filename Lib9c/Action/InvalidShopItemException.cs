using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidShopItemException : InvalidOperationException
    {
        public InvalidShopItemException(string s) : base(s)
        {
        }

        protected InvalidShopItemException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
