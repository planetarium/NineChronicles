using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Model.State;
using Nekoyume.State.Subjects;
using Debug = UnityEngine.Debug;
using static Lib9c.SerializeKeys;

namespace Nekoyume.State
{
    /// <summary>
    /// 클라이언트가 참조할 상태를 포함한다.
    /// 체인의 상태를 Setter를 통해서 받은 후, 로컬의 상태로 필터링해서 사용한다.
    /// </summary>
    public class States
    {
        public static States Instance => Game.Game.instance.States;

        public readonly Dictionary<Address, RankingMapState> RankingMapStates = new Dictionary<Address, RankingMapState>();

        public WeeklyArenaState WeeklyArenaState { get; private set; }

        public AgentState AgentState { get; private set; }

        public GoldBalanceState GoldBalanceState { get; private set; }

        private readonly Dictionary<int, AvatarState> _avatarStates = new Dictionary<int, AvatarState>();

        public IReadOnlyDictionary<int, AvatarState> AvatarStates => _avatarStates;

        public int CurrentAvatarKey { get; private set; }

        public AvatarState CurrentAvatarState { get; private set; }

        public GameConfigState GameConfigState { get; private set; }

        private readonly Dictionary<int, CombinationSlotState> _combinationSlotStates =
            new Dictionary<int, CombinationSlotState>();

        public States()
        {
            DeselectAvatar();
        }

        #region Setter

        /// <summary>
        /// 랭킹 상태를 할당한다.
        /// </summary>
        /// <param name="state"></param>
        public void SetRankingMapStates(RankingMapState state)
        {
            if (state is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(SetRankingMapStates)}] {nameof(state)} is null.");
                return;
            }

            RankingMapStates[state.address] = state;
            RankingMapStatesSubject.OnNext(RankingMapStates);
        }

        public void SetWeeklyArenaState(WeeklyArenaState state)
        {
            if (state is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(SetWeeklyArenaState)}] {nameof(state)} is null.");
                return;
            }

            LocalLayer.Instance.InitializeWeeklyArena(state);
            WeeklyArenaState = LocalLayer.Instance.Modify(state);
            WeeklyArenaStateSubject.OnNext(WeeklyArenaState);
        }

        /// <summary>
        /// 에이전트 상태를 할당한다.
        /// 로컬 세팅을 거친 상태가 최종적으로 할당된다.
        /// 최초로 할당하거나 기존과 다른 주소의 에이전트를 할당하면, 모든 아바타 상태를 새롭게 할당된다.
        /// </summary>
        /// <param name="state"></param>
        public void SetAgentState(AgentState state)
        {
            if (state is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(SetAgentState)}] {nameof(state)} is null.");
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
                AddOrReplaceAvatarState(pair.Value, pair.Key);
            }
        }

        public void SetGoldBalanceState(GoldBalanceState goldBalanceState)
        {
            if (goldBalanceState is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(SetGoldBalanceState)}] {nameof(goldBalanceState)} is null.");
                return;
            }

            GoldBalanceState = LocalLayer.Instance.Modify(goldBalanceState);
            AgentStateSubject.OnNextGold(GoldBalanceState.Gold);
        }

        public AvatarState AddOrReplaceAvatarState(
            Address avatarAddress,
            int index,
            bool initializeReactiveState = true) =>
            TryGetAvatarState(avatarAddress, true, out var avatarState)
                ? AddOrReplaceAvatarState(avatarState, index, initializeReactiveState)
                : null;

        public static bool TryGetAvatarState(Address address, out AvatarState avatarState) =>
            TryGetAvatarState(address, false, out avatarState);

        public static bool TryGetAvatarState(Address address, bool allowBrokenState, out AvatarState avatarState)
        {
            try
            {
                avatarState = GetAvatarState(address, allowBrokenState);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{e.GetType().FullName}: {e.Message} address({address.ToHex()})\n{e.StackTrace}");
                avatarState = null;
                return false;
            }
        }

        private static AvatarState GetAvatarState(Address address, bool allowBrokenState)
        {
            var agent = Game.Game.instance.Agent;
            var avatarStateValue = agent.GetState(address);
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
                var value = agent.GetState(address2);
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

        /// <summary>
        /// 아바타 상태를 할당한다.
        /// 로컬 세팅을 거친 상태가 최종적으로 할당된다.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="index"></param>
        /// <param name="initializeReactiveState"></param>
        public AvatarState AddOrReplaceAvatarState(AvatarState state, int index, bool initializeReactiveState = true)
        {
            if (state is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(AddOrReplaceAvatarState)}] {nameof(state)} is null.");
                return null;
            }

            if (AgentState is null || !AgentState.avatarAddresses.ContainsValue(state.address))
                throw new Exception(
                    $"`AgentState` is null or not found avatar's address({state.address}) in `AgentState`");

            state = LocalLayer.Instance.Modify(state);

            if (_avatarStates.ContainsKey(index))
            {
                _avatarStates[index] = state;
            }
            else
            {
                _avatarStates.Add(index, state);
            }

            return index == CurrentAvatarKey
                ? SelectAvatar(index, initializeReactiveState)
                : state;
        }

        /// <summary>
        /// 인자로 받은 인덱스의 아바타 상태를 제거한다.
        /// </summary>
        /// <param name="index"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        public void RemoveAvatarState(int index)
        {
            if (!_avatarStates.ContainsKey(index))
                throw new KeyNotFoundException($"{nameof(index)}({index})");

            _avatarStates.Remove(index);

            if (index == CurrentAvatarKey)
            {
                DeselectAvatar();
            }
        }

        /// <summary>
        /// 인자로 받은 인덱스의 아바타 상태를 선택한다.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="initializeReactiveState"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public AvatarState SelectAvatar(int index, bool initializeReactiveState = true)
        {
            if (!_avatarStates.ContainsKey(index))
            {
                throw new KeyNotFoundException($"{nameof(index)}({index})");
            }

            var isNew = CurrentAvatarKey != index;

            CurrentAvatarKey = index;
            var avatarState = _avatarStates[CurrentAvatarKey];
            LocalLayer.Instance.InitializeCurrentAvatarState(avatarState);
            UpdateCurrentAvatarState(avatarState, initializeReactiveState);

            if (isNew)
            {
                if (!TryGetAvatarState(avatarState.address, out var curAvatarState))
                {
                    return null;
                }

                AddOrReplaceAvatarState(curAvatarState, CurrentAvatarKey);
                SetCombinationSlotStates(curAvatarState);
            }

            if (Game.Game.instance.Agent is RPCAgent agent)
            {
                agent.UpdateSubscribeAddresses();
            }

            return CurrentAvatarState;
        }

        /// <summary>
        /// 아바타 상태 선택을 해지한다.
        /// </summary>
        public void DeselectAvatar()
        {
            CurrentAvatarKey = -1;
            LocalLayer.Instance?.InitializeCurrentAvatarState(null);
            UpdateCurrentAvatarState(null);
        }

        private void SetCombinationSlotStates(AvatarState avatarState)
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
                var state = new CombinationSlotState((Dictionary) Game.Game.instance.Agent.GetState(slotAddress));
                UpdateCombinationSlotState(i, state);
            }
        }

        public void UpdateCombinationSlotState(int index, CombinationSlotState state)
        {
            if (_combinationSlotStates.ContainsKey(index))
            {
                _combinationSlotStates[index] = state;
            }
            else
            {
                _combinationSlotStates.Add(index, state);
            }
        }

        public void RemoveSlotState(int index)
        {
            if (_combinationSlotStates.ContainsKey(index))
            {
                _combinationSlotStates.Remove(index);
            }
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

        public void SetGameConfigState(GameConfigState state)
        {
            GameConfigState = state;
            GameConfigStateSubject.OnNext(state);
        }

        #endregion

        /// <summary>
        /// `CurrentAvatarKey`에 따라서 `CurrentAvatarState`를 업데이트 한다.
        /// </summary>
        private void UpdateCurrentAvatarState(AvatarState state, bool initializeReactiveState = true)
        {
            CurrentAvatarState = state;

            if (!initializeReactiveState)
                return;

            ReactiveAvatarState.Initialize(CurrentAvatarState);
        }
    }
}
