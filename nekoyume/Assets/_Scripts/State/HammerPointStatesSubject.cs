using System;
using System.Collections.Generic;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.State
{
    public static class HammerPointStatesSubject
    {
        private static ReactiveDictionary<int, HammerPointState> _hammerPointStates;

        public static IObservable<DictionaryReplaceEvent<int, HammerPointState>> ObservableHammerPointStates
        {
            get
            {
                _hammerPointStates ??= new ReactiveDictionary<int, HammerPointState>(
                    (Dictionary<int, HammerPointState>) States.Instance.HammerPointStates);

                return _hammerPointStates.ObserveReplace();
            }
        }

        public static void UpdateHammerPointStates(int recipeId, HammerPointState state)
        {
            if (Addresses.GetHammerPointStateAddress(
                    States.Instance.CurrentAvatarState.address,
                    recipeId) == state.Address)
            {
                if (_hammerPointStates.ContainsKey(recipeId))
                {
                    _hammerPointStates[recipeId] = state;
                }
                else
                {
                    _hammerPointStates.Add(recipeId, state);
                }
            }
        }
    }
}
