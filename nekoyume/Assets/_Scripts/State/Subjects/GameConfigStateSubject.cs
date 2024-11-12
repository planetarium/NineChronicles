using System;
using Libplanet.Crypto;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.State.Subjects
{
    public static class GameConfigStateSubject
    {
        private static readonly Subject<GameConfigState> GameConfigStateInternal;
        
        public static readonly IObservable<GameConfigState> GameConfigState;

        public static readonly ReactiveDictionary<Address, bool> ActionPointState = new();
        
        static GameConfigStateSubject()
        {
            GameConfigStateInternal = new Subject<GameConfigState>();
            GameConfigState = GameConfigStateInternal.ObserveOnMainThread();
        }

        public static void OnNext(GameConfigState state)
        {
            GameConfigStateInternal.OnNext(state);
        }
    }
}
