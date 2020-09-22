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

        public NotEnoughFungibleAssetValueException(string require, string current)
            : this(
                $"Aborted as the signer's balance is insufficient to pay entrance fee/stake: {current} < {require}.")
        {
        }

        public NotEnoughFungibleAssetValueException(
            FungibleAssetValue require,
            FungibleAssetValue current) : this(require.ToString(), current.ToString())
        {
        }

        public NotEnoughFungibleAssetValueException(
            FungibleAssetValue require,
            BigInteger current)
            : this(require.ToString(), current.ToString(CultureInfo.InvariantCulture))
        {
        }

        public NotEnoughFungibleAssetValueException(
            BigInteger require,
            FungibleAssetValue current)
            : this(require.ToString(CultureInfo.InvariantCulture), current.ToString())
        {
        }

        public NotEnoughFungibleAssetValueException(
            BigInteger require,
            BigInteger current)
            : this(
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
