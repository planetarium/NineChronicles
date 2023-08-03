#nullable enable

using System;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using Libplanet.Common;
using Libplanet.Crypto;

namespace Nekoyume.Exceptions
{
    [Serializable]
    public class ItemNotFoundException : Exception
    {
        public ItemNotFoundException()
        {
        }

        public ItemNotFoundException(string? message) : base(message)
        {
        }

        public ItemNotFoundException(string? message, Exception? innerException) :
            base(message, innerException)
        {
        }

        public ItemNotFoundException(
            Address inventoryAddr,
            HashDigest<SHA256> fungibleId,
            Exception? innerException = null) :
            base($"Item not found: {inventoryAddr} {fungibleId}", innerException)
        {
        }

        public ItemNotFoundException(
            Address inventoryAddr,
            Guid nonFungibleId,
            Exception? innerException = null) :
            base($"Item not found: {inventoryAddr} {nonFungibleId}", innerException)
        {
        }

        protected ItemNotFoundException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
