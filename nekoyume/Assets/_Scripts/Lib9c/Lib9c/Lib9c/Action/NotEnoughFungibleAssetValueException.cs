using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.Serialization;
using Libplanet.Assets;

namespace Nekoyume.Action
{
    [Serializable]
    public class NotEnoughFungibleAssetValueException : Exception
    {
        public NotEnoughFungibleAssetValueException(string message) : base(message)
        {
        }

        public NotEnoughFungibleAssetValueException(string addressesHex, string require, string current)
            : base(
                $"{addressesHex}Aborted as the signer's balance is insufficient to pay entrance fee/stake: {current} < {require}.")
        {
        }

        public NotEnoughFungibleAssetValueException(
            string addressesHex,
            FungibleAssetValue require,
            FungibleAssetValue current) : this(addressesHex, require.ToString(), current.ToString())
        {
        }

        public NotEnoughFungibleAssetValueException(
            string addressesHex,
            FungibleAssetValue require,
            BigInteger current) : this(addressesHex, require.ToString(), current.ToString(CultureInfo.InvariantCulture))
        {
        }

        public NotEnoughFungibleAssetValueException(
            string addressesHex,
            BigInteger require,
            FungibleAssetValue current) : this(addressesHex, require.ToString(CultureInfo.InvariantCulture), current.ToString())
        {
        }

        public NotEnoughFungibleAssetValueException(
            string addressesHex,
            BigInteger require,
            BigInteger current)
            : this(addressesHex, require.ToString(CultureInfo.InvariantCulture), current.ToString(CultureInfo.InvariantCulture))
        {
        }

        public NotEnoughFungibleAssetValueException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
