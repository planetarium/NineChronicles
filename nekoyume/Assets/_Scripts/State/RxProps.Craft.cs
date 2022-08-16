using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet;
using Nekoyume.Model.State;
using Nekoyume.UI;
using UniRx;
using UnityEngine.TextCore.LowLevel;

namespace Nekoyume.State
{
    public static partial class RxProps
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

                UniTask.Run(async () =>
                {
                    var states = await InitializeHammerPointStates();
                    if (states is null)
                    {
                        return;
                    }

                    _hammerPointStates =
                        new ReactiveDictionary<int, HammerPointState>(states);
                });
                return _hammerPointStates;
            }
        }

        public static void UpdateHammerPointStates(int recipeId)
        {
            if (!_currentAvatarAddress.HasValue)
            {
                return;
            }

            var address =
                Addresses.GetHammerPointStateAddress(_currentAvatarAddress.Value, recipeId);
            var serialized = _agent.GetStateAsync(address).Result;
            var hammerPointState = new HammerPointState(address, serialized as List);
            if (_hammerPointStates.ContainsKey(recipeId))
            {
                _hammerPointStates[recipeId] = hammerPointState;
            }
            else
            {
                _hammerPointStates.Add(recipeId, hammerPointState);
            }
        }

        private static async Task<Dictionary<int, HammerPointState>> InitializeHammerPointStates()
        {
            if (_tableSheets.CrystalHammerPointSheet is null || !_currentAvatarAddress.HasValue)
            {
                return null;
            }

            var hammerPointStateAddresses =
                Craft.SharedModel.UnlockedRecipes.Value.Select(
                    recipeId =>
                        (Addresses.GetHammerPointStateAddress(
                            _currentAvatarAddress.Value,
                            recipeId), recipeId))
                    .ToList();
            var states =
                await _agent.GetStateBulk(hammerPointStateAddresses.Select(tuple => tuple.Item1));
            var joinedStates = states.Join(
                hammerPointStateAddresses,
                state => state.Key,
                tuple => tuple.Item1,
                (state, tuple) => (state, tuple.recipeId));

            return joinedStates
                .Select(tuple =>
                {
                    var state = tuple.state;
                    if (state.Value is not null)
                    {
                        return new HammerPointState(state.Key, (List)state.Value);
                    }

                    return new HammerPointState(state.Key, tuple.recipeId);
                })
                .ToDictionary(value => value.RecipeId, value => value);
        }
    }
}
