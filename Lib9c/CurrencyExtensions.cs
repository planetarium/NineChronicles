using System;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;

namespace Nekoyume
{
    public static class CurrencyExtensions
    {
        public static Bencodex.Types.Dictionary Serialize(this Currency currency)
        {
            IValue minters = currency.Minters is IImmutableSet<Address> m
                ? new Bencodex.Types.List(m.Select<Address, IValue>(a => new Binary(a.ByteArray)))
                : (IValue)Null.Value;
            var serialized = Bencodex.Types.Dictionary.Empty
                .Add("ticker", currency.Ticker)
                .Add("minters", minters)
                .Add("decimalPlaces", new[] { currency.DecimalPlaces });
            if (currency.TotalSupplyTrackable)
            {
                serialized = serialized.Add("totalSupplyTrackable", true);
                if (currency.MaximumSupply is { } maximumSupply)
                {
                    serialized = serialized.Add(
                        "maximumSupplyMajor",
                        (IValue)new Integer(maximumSupply.MajorUnit)
                        ).Add(
                        "maximumSupplyMinor",
                        (IValue)new Integer(maximumSupply.MinorUnit));
                }
            }

            return serialized;
        }

        public static Currency Deserialize(Bencodex.Types.Dictionary serialized)
        {
            IImmutableSet<Address> minters = null;
            if (serialized["minters"] is Bencodex.Types.List mintersAsList)
            {
                minters = mintersAsList.Select(b => new Address(((Binary) b).ByteArray)).ToImmutableHashSet();
            }

            if (serialized.ContainsKey("totalSupplyTrackable"))
            {
                if (serialized.ContainsKey("maximumSupplyMajor")
                    && serialized.ContainsKey("maximumSupplyMinor"))
                {
                    return Currency.Capped(
                        (Text)serialized["ticker"],
                        ((Binary)serialized["decimalPlaces"]).First(),
                        (
                            (Integer)serialized["maximumSupplyMajor"],
                            (Integer)serialized["maximumSupplyMinor"]
                        ),
                        minters);
                }

                if (!serialized.ContainsKey("maximumSupplyMajor")
                    && !serialized.ContainsKey("maximumSupplyMinor"))
                {
                    return Currency.Uncapped(
                        (Text)serialized["ticker"],
                        ((Binary)serialized["decimalPlaces"]).First(),
                        minters);
                }

                throw new ArgumentException(
                    "Both \"maximumSupplyMajor\" and \"maximumSupplyMinor\" must be "
                    + " omitted or be non-negative integers.",
                    nameof(serialized));
            }

            if (serialized.ContainsKey("maximumSupplyMajor")
                || serialized.ContainsKey("maximumSupplyMinor"))
            {
                throw new ArgumentException(
                    "\"maximumSupplyMajor\" and \"maximumSupplyMinor\" may only be present "
                    + " on non-legacy trackable currencies.",
                    nameof(serialized));
            }
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            return Currency.Legacy(
                (Text)serialized["ticker"],
                ((Binary)serialized["decimalPlaces"]).First(),
                minters);
#pragma warning restore CS0618
        }
    }
}
