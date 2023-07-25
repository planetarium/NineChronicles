#nullable enable

using System;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using Libplanet.Common;
using Libplanet.Crypto;

namespace Nekoyume.Exceptions
{
    [Serializable]
    public class NotEnoughItemException : Exception
    {
        public NotEnoughItemException()
        {
        }

        public NotEnoughItemException(string? message) : base(message)
        {
        }

        public NotEnoughItemException(string? message, Exception? innerException) :
            base(message, innerException)
        {
        }

        public NotEnoughItemException(
            Address inventoryAddr,
            HashDigest<SHA256> fungibleId,
            int expectedCount,
            int actualCount,
            Exception? innerException = null) :
            base(
                $"Not enough item: {inventoryAddr} {fungibleId}, " +
                $"expected: {expectedCount}, actual: {actualCount}",
                innerException)
        {
        }

        protected NotEnoughItemException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
