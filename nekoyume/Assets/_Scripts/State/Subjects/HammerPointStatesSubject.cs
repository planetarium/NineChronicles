using System;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.State.Subjects
{
    public static class HammerPointStatesSubject
    {
        private static readonly Subject<(int, HammerPointState)> _hammerPointSubject;

        public static readonly IObservable<(int, HammerPointState)> HammerPointSubject;

        static HammerPointStatesSubject()
        {
            _hammerPointSubject = new Subject<(int, HammerPointState)>();
            HammerPointSubject = _hammerPointSubject.ObserveOnMainThread();
        }

        public static void OnReplaceHammerPointState(int recipeId, HammerPointState state)
        {
            if (Addresses.GetHammerPointStateAddress(
                    States.Instance.CurrentAvatarState.address,
                    recipeId) == state.Address)
            {
                _hammerPointSubject.OnNext((recipeId, state));
            }
        }
    }
}
