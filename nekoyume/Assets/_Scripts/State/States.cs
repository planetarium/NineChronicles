using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Model.State;
using Nekoyume.State.Subjects;
using Debug = UnityEngine.Debug;
using static Lib9c.SerializeKeys;

namespace Nekoyume.State
{
    public class States
    {
        public static States Instance => Game.Game.instance.States;

        /// <summary>
        /// Update when every block rendered by the UpdateAsync() method by the UpdateAsync() method.
        /// </summary>
        public GoldBalanceState GoldBalanceState { get; private set; }

        /// <summary>
        /// Update if updated this address when each block rendered by the UpdateAsync() method.
        /// </summary>
        public AgentState AgentState { get; private set; }

        /// <summary>
        /// Update if updated this address when each block rendered by the UpdateAsync() method.
        /// </summary>
        private readonly Dictionary<int, AvatarState> _avatarStates = new Dictionary<int, AvatarState>();

        public IReadOnlyDictionary<int, AvatarState> AvatarStates => _avatarStates;

        /// <summary>
        /// Update if updated this address when each block rendered by the UpdateAsync() method or current avatar changed.
        /// </summary>
        private readonly Dictionary<int, CombinationSlotState> _combinationSlotStates =
            new Dictionary<int, CombinationSlotState>();

        public IReadOnlyDictionary<int, CombinationSlotState> CombinationSlotStates => _combinationSlotStates;

        /// <summary>
        /// Update if updated this address when each block rendered by the UpdateAsync() method.
        /// </summary>
        public GameConfigState GameConfigState { get; private set; }

        /// <summary>
        /// Update when click the refresh button of the `RankPopup` UI widget.
        /// </summary>
        public readonly Dictionary<Address, RankingMapState> RankingMapStates =
            new Dictionary<Address, RankingMapState>();

        /// <summary>
        /// Update when entering to the `RankingBoard` UI widget.
        /// </summary>
        public WeeklyArenaState WeeklyArenaState { get; private set; }

        public int CurrentAvatarKey { get; private set; }

        public AvatarState CurrentAvatarState { get; private set; }

        public States()
        {
            DeselectAvatar();
        }

        public async UniTask UpdateAsync(
            IAgent agent,
            IReadOnlyList<Address> updatedAddresses,
            bool ignoreNotify = false)
        {
            // Update gold balance in every update.
            var fungibleAssetValue = await agent.GetBalanceAsync(
                agent.Address,
                GoldBalanceState.Gold.Currency);
            SetGoldBalanceState(new GoldBalanceState(agent.Address, fungibleAssetValue), ignoreNotify);

            // Update the updated addresses.
            for (var i = 0; i < updatedAddresses.Count; i++)
            {
                var updatedAddress = updatedAddresses[i];
                // AgentState
                if (updatedAddress.Equals(AgentState.address))
                {
                    var value = await agent.GetStateAsync(AgentState.address);
                    if (!(value is Bencodex.Types.Dictionary dict))
                    {
                        Debug.LogError($"{nameof(value)} is not Bencodex.Type.Dictionary");
                        return;
                    }

                    AgentState = new AgentState(dict);
                    continue;
                }

                // AvatarState
                var hasUpdated = false;
                for (var j = 0; j < _avatarStates.Count; j++)
                {
                    if (!_avatarStates.ContainsKey(j))
                    {
                        continue;
                    }

                    var avatarState = _avatarStates[j];
                    if (!updatedAddress.Equals(avatarState.address))
                    {
                        continue;
                    }

                    await AddOrReplaceAvatarStateAsync(avatarState.address, j);
                    hasUpdated = true;
                    break;
                }

                if (hasUpdated)
                {
                    continue;
                }
                
                // CombinationSlotStates
                foreach (var pair in _combinationSlotStates)
                {
                    var combinationSlotState = pair.Value;
                    if (!updatedAddress.Equals(combinationSlotState.address))
                    {
                        continue;
                    }

                    var value = await agent.GetStateAsync(combinationSlotState.address);
                    if (!(value is Bencodex.Types.Dictionary dict))
                    {
                        Debug.LogError($"{nameof(value)} is not Bencodex.Type.Dictionary");
                        return;
                    }

                    var state = new CombinationSlotState(dict);
                    AddOrReplaceCombinationSlotState(pair.Key, state);
                    hasUpdated = true;
                    break;
                }

                if (hasUpdated)
                {
                    continue;
                }

                // GameConfigState
                if (updatedAddress.Equals(GameConfigState.address))
                {
                    var value = await agent.GetStateAsync(GameConfigState.address);
                    if (!(value is Bencodex.Types.Dictionary dict))
                    {
                        Debug.LogError($"{nameof(value)} is not Bencodex.Type.Dictionary");
                        return;
                    }

                    var state = new GameConfigState(dict);
                    SetGameConfigState(state);
                }
            }
        }

        #region Setter

        public void SetGoldBalanceState(GoldBalanceState goldBalanceState, bool ignoreNotify = false)
        {
            if (goldBalanceState is null)
            {
                Debug.LogWarning(
                    $"[{nameof(States)}.{nameof(SetGoldBalanceState)}] {nameof(goldBalanceState)} is null.");
                return;
            }

            GoldBalanceState = LocalLayer.Instance.Modify(goldBalanceState);

            if (ignoreNotify)
            {
                return;
            }

            AgentStateSubject.OnNextGold(GoldBalanceState.Gold);
        }

        public async UniTask SetAgentStateAsync(AgentState state, bool ignoreNotify = false)
        {
            if (state is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(SetAgentStateAsync)}] {nameof(state)} is null.");
                return;
            }

            var getAllOfAvatarStates =
                AgentState is null ||
                !AgentState.address.Equals(state.address);

            LocalLayer.Instance.InitializeAgentAndAvatars(state);
            AgentState = LocalLayer.Instance.Modify(state);

            if (!getAllOfAvatarStates)
            {
                return;
            }

            foreach (var pair in AgentState.avatarAddresses)
            {
                await AddOrReplaceAvatarStateAsync(pair.Value, pair.Key, ignoreNotify);
            }
        }

        private void SetCurrentAvatarState(AvatarState state, bool ignoreNotify = false)
        {
            CurrentAvatarState = state;

            if (ignoreNotify)
            {
                return;
            }

            ReactiveAvatarState.Initialize(CurrentAvatarState);
        }

        public async UniTask<AvatarState> AddOrReplaceAvatarStateAsync(
            Address avatarAddress,
            int index,
            bool ignoreNotify = false)
        {
            var (exist, avatarState) = await TryGetAvatarStateAsync(avatarAddress, true);
            if (exist)
            {
                await AddOrReplaceAvatarStateAsync(avatarState, index, ignoreNotify);
            }

            return null;
        }

        public async UniTask<AvatarState> AddOrReplaceAvatarStateAsync(
            AvatarState state,
            int index,
            bool ignoreNotify = false)
        {
            if (state is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(AddOrReplaceAvatarStateAsync)}] {nameof(state)} is null.");
                return null;
            }

            if (AgentState is null || !AgentState.avatarAddresses.ContainsValue(state.address))
            {
                throw new Exception(
                    $"`AgentState` is null or not found avatar's address({state.address}) in `AgentState`");
            }

            state = LocalLayer.Instance.Modify(state);

            if (_avatarStates.ContainsKey(index))
            {
                _avatarStates[index] = state;
            }
            else
            {
                _avatarStates.Add(index, state);
            }

            if (index == CurrentAvatarKey)
            {
                return await UniTask.Run(async () => await SelectAvatarAsync(index, ignoreNotify));
            }

            return state;
        }

        public void SetGameConfigState(GameConfigState state, bool ignoreNotify = false)
        {
            GameConfigState = state;

            if (ignoreNotify)
            {
                return;
            }

            GameConfigStateSubject.OnNext(GameConfigState);
        }

        private async UniTask SetCombinationSlotStatesAsync(AvatarState avatarState, bool ignoreNotify = false)
        {
            if (avatarState is null)
            {
                LocalLayer.Instance.InitializeCombinationSlotsByCurrentAvatarState(null);
                return;
            }

            LocalLayer.Instance.InitializeCombinationSlotsByCurrentAvatarState(avatarState);
            for (var i = 0; i < avatarState.combinationSlotAddresses.Count; i++)
            {
                var slotAddress = avatarState.address.Derive(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CombinationSlotState.DeriveFormat,
                        i
                    )
                );
                var stateValue = await Game.Game.instance.Agent.GetStateAsync(slotAddress);
                var state = new CombinationSlotState((Dictionary)stateValue);
                AddOrReplaceCombinationSlotState(i, state, ignoreNotify);
            }
        }

        public void AddOrReplaceCombinationSlotState(int index, CombinationSlotState state, bool ignoreNotify = false)
        {
            if (_combinationSlotStates.ContainsKey(index))
            {
                _combinationSlotStates[index] = state;
            }
            else
            {
                _combinationSlotStates.Add(index, state);
            }

            if (ignoreNotify)
            {
                return;
            }

            // TODO: notify
        }

        public Dictionary<int, CombinationSlotState> GetCombinationSlotState(long currentBlockIndex)
        {
            if (_combinationSlotStates == null)
            {
                return new Dictionary<int, CombinationSlotState>();
            }

            return _combinationSlotStates
                .Where(pair => !pair.Value.Validate(CurrentAvatarState, currentBlockIndex))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public void SetRankingMapStates(RankingMapState state, bool ignoreNotify = false)
        {
            if (state is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(SetRankingMapStates)}] {nameof(state)} is null.");
                return;
            }

            RankingMapStates[state.address] = state;

            if (ignoreNotify)
            {
                return;
            }

            RankingMapStatesSubject.OnNext(RankingMapStates);
        }

        public void SetWeeklyArenaState(WeeklyArenaState state, bool ignoreNotify = false)
        {
            if (state is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(SetWeeklyArenaState)}] {nameof(state)} is null.");
                return;
            }

            LocalLayer.Instance.InitializeWeeklyArena(state);
            WeeklyArenaState = LocalLayer.Instance.Modify(state);

            if (ignoreNotify)
            {
                return;
            }

            WeeklyArenaStateSubject.OnNext(WeeklyArenaState);
        }

        #endregion

        #region Getter

        public static async UniTask<(bool exist, AvatarState avatarState)> TryGetAvatarStateAsync(Address address) =>
            await TryGetAvatarStateAsync(address, false);

        public static async UniTask<(bool exist, AvatarState avatarState)> TryGetAvatarStateAsync(Address address,
            bool allowBrokenState)
        {
            AvatarState avatarState = null;
            bool exist = false;
            try
            {
                avatarState = await GetAvatarStateAsync(address, allowBrokenState);
                exist = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{e.GetType().FullName}: {e.Message} address({address.ToHex()})\n{e.StackTrace}");
            }

            return (exist, avatarState);
        }

        private static async UniTask<AvatarState> GetAvatarStateAsync(Address address, bool allowBrokenState)
        {
            var agent = Game.Game.instance.Agent;
            var avatarStateValue = await agent.GetStateAsync(address);
            if (!(avatarStateValue is Bencodex.Types.Dictionary dict))
            {
                Debug.LogWarning("Failed to get AvatarState");
                throw new FailedLoadStateException($"Failed to get AvatarState: {address.ToHex()}");
            }

            if (dict.ContainsKey(LegacyNameKey))
            {
                return new AvatarState(dict);
            }

            foreach (var key in new[]
            {
                LegacyInventoryKey,
                LegacyWorldInformationKey,
                LegacyQuestListKey,
            })
            {
                var address2 = address.Derive(key);
                var value = await agent.GetStateAsync(address2);
                if (value is null)
                {
                    if (allowBrokenState &&
                        dict.ContainsKey(key))
                    {
                        dict = new Bencodex.Types.Dictionary(dict.Remove((Text)key));
                    }

                    continue;
                }

                dict = dict.SetItem(key, value);
            }

            return new AvatarState(dict);
        }

        #endregion

        #region CurrentAvatarState

        public async UniTask<AvatarState> SelectAvatarAsync(int index, bool ignoreNotify = false)
        {
            if (!_avatarStates.ContainsKey(index))
            {
                throw new KeyNotFoundException($"{nameof(index)}({index})");
            }

            var isNew = CurrentAvatarKey != index;

            CurrentAvatarKey = index;
            var avatarState = _avatarStates[CurrentAvatarKey];
            LocalLayer.Instance.InitializeCurrentAvatarState(avatarState);
            SetCurrentAvatarState(avatarState, ignoreNotify);

            if (isNew)
            {
                _combinationSlotStates.Clear();
                await UniTask.Run(async () =>
                {
                    await SetCombinationSlotStatesAsync(avatarState);
                    await AddOrReplaceAvatarStateAsync(avatarState, CurrentAvatarKey, ignoreNotify);
                });
            }

            if (Game.Game.instance.Agent is RPCAgent agent)
            {
                agent.UpdateSubscribeAddresses();
            }

            return CurrentAvatarState;
        }

        private void DeselectAvatar()
        {
            CurrentAvatarKey = -1;
            LocalLayer.Instance?.InitializeCurrentAvatarState(null);
            SetCurrentAvatarState(null);
        }

        #endregion
    }
}
