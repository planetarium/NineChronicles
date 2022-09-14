using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Nekoyume.Game;
using Nekoyume.Model.State;
using Nekoyume.UI;
using UniRx;

namespace Nekoyume.State
{
    public static class ReactiveHammerPointStates
    {
        private static ReactiveDictionary<int, HammerPointState> _hammerPointStates;

        public static ReactiveDictionary<int, HammerPointState> HammerPointStates
        {
            get
            {
                if (_hammerPointStates is not null)
                {
                    return _hammerPointStates;
                }

                return _hammerPointStates =
                    new ReactiveDictionary<int, HammerPointState>(
                        States.Instance.HammerPointStates);
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
