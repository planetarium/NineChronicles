using System;
using System.Runtime.Serialization;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class FailedToUnregisterInShopStateException : Exception
    {
        public FailedToUnregisterInShopStateException(string message) : base(message)
        {
        }

        protected FailedToUnregisterInShopStateException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
