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
            return Bencodex.Types.Dictionary.Empty
                .Add("ticker", currency.Ticker)
                .Add("minters", minters)
                .Add("decimalPlaces", new[] { currency.DecimalPlaces });
        }

        public static Currency Deserialize(Bencodex.Types.Dictionary serialized)
        {
            IImmutableSet<Address> minters = null;
            if (serialized["minters"] is Bencodex.Types.List mintersAsList)
            {
                minters = mintersAsList.Select(b => new Address(((Binary) b).ByteArray)).ToImmutableHashSet();
            }
            
            return new Currency((Text)serialized["ticker"], ((Binary)serialized["decimalPlaces"]).First(), minters);
        }
    }
}
