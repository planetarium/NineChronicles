using System;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.State.Subjects
{
    public static class HammerPointStatesSubject
    {
        private static readonly Subject<(int, HammerPointState)> HammerPointSubjectInternal;

        public static readonly IObservable<(int, HammerPointState)> HammerPoint;

        static HammerPointStatesSubject()
        {
            HammerPointSubjectInternal = new Subject<(int, HammerPointState)>();
            HammerPoint = HammerPointSubjectInternal.ObserveOnMainThread();
        }

        public static void OnReplaceHammerPointState(int recipeId, HammerPointState state)
        {
            if (Addresses.GetHammerPointStateAddress(
                    States.Instance.CurrentAvatarState.address,
                    recipeId) == state.Address)
            {
                HammerPointSubjectInternal.OnNext((recipeId, state));
            }
        }
    }
}
