using System;
using Lib9c;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.Stake;
using UniRx;

namespace Nekoyume.State.Subjects
{
    public static class BalanceSubject
    {
        private static readonly Subject<(Address address, FungibleAssetValue balance)> Subject = new();

        public static void OnNextBalance(Address address, FungibleAssetValue balance) =>
            Subject.OnNext((address, balance));

        public static void OnNextAgentBalance(FungibleAssetValue balance) =>
            OnNextBalance(States.Instance.AgentState.address, balance);

        public static void OnNextAgentStakedNCG(FungibleAssetValue balance) =>
            OnNextBalance(
                StakeStateV2.DeriveAddress(States.Instance.AgentState.address),
                balance);

        public static void OnNextCurrentAvatarBalance(FungibleAssetValue balance) =>
            OnNextBalance(States.Instance.CurrentAvatarState.address, balance);

        public static IObservable<FungibleAssetValue> Observe(
            Address address,
            Currency currency) => Subject
            .ObserveOnMainThread()
            .Where(tuple =>
                tuple.address.Equals(address) &&
                tuple.balance.Currency.Equals(currency))
            .Select(tuple => tuple.balance);

        public static IObservable<FungibleAssetValue> ObserveAgentNCG() =>
            Observe(States.Instance.AgentState.address, States.Instance.NCG);

        public static IObservable<FungibleAssetValue> ObserveAgentCrystal() =>
            Observe(States.Instance.AgentState.address, Currencies.Crystal);

        public static IObservable<FungibleAssetValue> ObserveAgentGarage() =>
            Observe(States.Instance.AgentState.address, Currencies.Garage);
    }
}
