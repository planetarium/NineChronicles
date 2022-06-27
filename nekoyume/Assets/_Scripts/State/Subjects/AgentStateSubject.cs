using System;
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
            
        public static readonly IObservable<FungibleAssetValue> Gold;

        private static readonly Subject<FungibleAssetValue> _crystal;

        public static readonly IObservable<FungibleAssetValue> Crystal;

        static AgentStateSubject()
        {
            _gold = new Subject<FungibleAssetValue>();
            Gold = _gold.ObserveOnMainThread();
            _crystal = new Subject<FungibleAssetValue>();
            Crystal = _crystal.ObserveOnMainThread();
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
            if (crystal.Currency.Equals(CrystalCalculator.CRYSTAL))
            {
                _crystal.OnNext(crystal);
            }
        }
    }
}
