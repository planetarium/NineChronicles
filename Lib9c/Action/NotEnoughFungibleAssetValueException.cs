using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.Serialization;
using Libplanet.Types.Assets;

namespace Nekoyume.Action
{
    [Serializable]
    public class NotEnoughFungibleAssetValueException : Exception
    {
        public NotEnoughFungibleAssetValueException(
            string message,
            Exception innerException = null) :
            base(message, innerException)
        {
        }

        public NotEnoughFungibleAssetValueException(
            string addressesHex,
            string require,
            string current,
            Exception innerException = null)
            : this(
                $"{addressesHex}Aborted as the signer's balance is" +
                $" insufficient to pay entrance fee/stake: {current} < {require}.",
                innerException)
        {
        }

        public NotEnoughFungibleAssetValueException(
            string actionType,
            string addressesHex,
            string require,
            string current,
            Exception innerException = null)
            : this(
                $"[{actionType}][{addressesHex}] Aborted as the signer's balance is" +
                $" insufficient to pay entrance fee/stake: {current} < {require}.",
                innerException)
        {
        }

        public NotEnoughFungibleAssetValueException(
            string actionType,
            string addressesHex,
            FungibleAssetValue require,
            FungibleAssetValue current) :
            this(actionType, addressesHex, require.ToString(), current.ToString())
        {
        }

        public NotEnoughFungibleAssetValueException(
            string actionType,
            string addressesHex,
            FungibleAssetValue require,
            BigInteger current) :
            this(
                actionType,
                addressesHex,
                require.ToString(),
                current.ToString(CultureInfo.InvariantCulture))
        {
        }

        public NotEnoughFungibleAssetValueException(
            string addressesHex,
            BigInteger require,
            FungibleAssetValue current) :
            this(
                addressesHex,
                require.ToString(CultureInfo.InvariantCulture),
                current.ToString())
        {
        }

        public NotEnoughFungibleAssetValueException(
            string actionType,
            string addressesHex,
            BigInteger require,
            FungibleAssetValue current) :
            this(
                actionType,
                addressesHex,
                require.ToString(CultureInfo.InvariantCulture),
                current.ToString())
        {
        }

        public NotEnoughFungibleAssetValueException(
            string actionType,
            string addressesHex,
            BigInteger require,
            BigInteger current)
            : this(
                actionType,
                addressesHex,
                require.ToString(CultureInfo.InvariantCulture),
                current.ToString(CultureInfo.InvariantCulture))
        {
        }

        public NotEnoughFungibleAssetValueException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
