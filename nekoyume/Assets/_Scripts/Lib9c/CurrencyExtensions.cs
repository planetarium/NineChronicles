using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;

namespace Nekoyume
{
    public static class CurrencyExtensions
    {
        public static Bencodex.Types.Dictionary Serialize(this Currency currency)
        {
            IValue minters = currency.Minters is IImmutableSet<Address> m
                ? new Bencodex.Types.List(m.Select<Address, IValue>(a => new Binary(a.ToByteArray())))
                : (IValue) default(Null);
            return Bencodex.Types.Dictionary.Empty
                .Add("ticker", currency.Ticker)
                .Add("minters", minters);
        }

        public static Currency Deserialize(Bencodex.Types.Dictionary serialized)
        {
            var minters = ((Bencodex.Types.List) serialized["minters"])
                .Select(b => new Address((Binary) b))
                .ToImmutableHashSet();
            return new Currency((Text) serialized["ticker"], minters);
        }
    }
}
