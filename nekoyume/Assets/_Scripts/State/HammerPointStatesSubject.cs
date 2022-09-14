using System;
using System.Collections.Generic;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.State
{
    public static class HammerPointStatesSubject
    {
        private static readonly ReactiveDictionary<int, HammerPointState> HammerPointStates = new(
            (Dictionary<int, HammerPointState>) States.Instance.HammerPointStates);

        public static IObservable<DictionaryReplaceEvent<int, HammerPointState>>
            ObservableHammerPointStates => HammerPointStates.ObserveReplace();

        public static void UpdateHammerPointStates(int recipeId, HammerPointState state)
        {
            if (Addresses.GetHammerPointStateAddress(
                    States.Instance.CurrentAvatarState.address,
                    recipeId) == state.Address)
            {
                if (HammerPointStates.ContainsKey(recipeId))
                {
                    HammerPointStates[recipeId] = state;
                }
                else
                {
                    HammerPointStates.Add(recipeId, state);
                }
            }
        }
    }
}
