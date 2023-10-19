using System;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using UniRx;

namespace Nekoyume.State.Subjects
{
    public static class BalanceSubject
    {
        private static readonly Subject<(Address address, FungibleAssetValue balance)> Subject = new();

        public static IObservable<FungibleAssetValue> Observe(
            Address address,
            Currency currency) => Subject
            .Where(tuple =>
                tuple.address.Equals(address) &&
                tuple.balance.Currency.Equals(currency))
            .Select(tuple => tuple.balance);

        public static void OnNextBalance(Address address, FungibleAssetValue balance) =>
            Subject.OnNext((address, balance));
    }
}
