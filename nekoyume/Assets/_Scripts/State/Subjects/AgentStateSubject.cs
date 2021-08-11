using System;
using Libplanet.Assets;
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

        static AgentStateSubject()
        {
            _gold = new Subject<FungibleAssetValue>();
            Gold = _gold.ObserveOnMainThread();
        }

        public static void OnNextGold(FungibleAssetValue gold)
        {
            _gold.OnNext(gold);
        }
    }
}
