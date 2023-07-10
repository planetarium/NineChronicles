using System;
using Lib9c;
using Libplanet.Assets;
using Nekoyume.Helper;
using UniRx;

namespace Nekoyume.State.Subjects
{
    /// <summary>
    /// The change of the value included in `AgentState` is notified to the outside through each Subject<T> field.
    /// </summary>
    public static class AgentStateSubject
    {
        private static readonly Subject<FungibleAssetValue> _gold;
        private static readonly Subject<FungibleAssetValue> _crystal;
        private static readonly Subject<FungibleAssetValue> _garage;

        public static readonly IObservable<FungibleAssetValue> Gold;
        public static readonly IObservable<FungibleAssetValue> Crystal;
        public static readonly IObservable<FungibleAssetValue> Garage;

        static AgentStateSubject()
        {
            _gold = new Subject<FungibleAssetValue>();
            _crystal = new Subject<FungibleAssetValue>();
            _garage = new Subject<FungibleAssetValue>();
            Gold = _gold.ObserveOnMainThread();
            Crystal = _crystal.ObserveOnMainThread();
            Garage = _garage.ObserveOnMainThread();
        }

        public static void OnNextGold(FungibleAssetValue gold)
        {
            if (gold.Currency.Equals(States.Instance.GoldBalanceState.Gold.Currency))
            {
                _gold.OnNext(gold);
            }
        }

        public static void OnNextCrystal(FungibleAssetValue crystal)
        {
            if (crystal.Currency.Equals(Currencies.Crystal))
            {
                _crystal.OnNext(crystal);
            }
        }

        public static void OnNextGarage(FungibleAssetValue garage)
        {
            if (garage.Currency.Equals(Currencies.Garage))
            {
                _garage.OnNext(garage);
            }
        }
    }
}
