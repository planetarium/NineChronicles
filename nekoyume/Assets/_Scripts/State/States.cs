using System;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Model.State;
using Nekoyume.State.Subjects;
using UnityEngine;

namespace Nekoyume.State
{
    /// <summary>
    /// 클라이언트가 참조할 상태를 포함한다.
    /// 체인의 상태를 Setter를 통해서 받은 후, 로컬의 상태로 필터링해서 사용한다.
    /// </summary>
    public class States
    {
        public static States Instance => Game.Game.instance.States;

        public RankingState RankingState { get; private set; }

        public ShopState ShopState { get; private set; }
        
        public WeeklyArenaState WeeklyArenaState { get; private set; }

        public AgentState AgentState { get; private set; }

        private readonly Dictionary<int, AvatarState> _avatarStates = new Dictionary<int, AvatarState>();
        public IReadOnlyDictionary<int, AvatarState> AvatarStates => _avatarStates;

        public int CurrentAvatarKey { get; private set; }

        public AvatarState CurrentAvatarState { get; private set; }

        public States()
        {
            DeselectAvatar();
        }

        #region Setter

        /// <summary>
        /// 랭킹 상태를 할당한다.
        /// </summary>
        /// <param name="state"></param>
        public void SetRankingState(RankingState state)
        {
            if (state is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(SetRankingState)}] {nameof(state)} is null.");
                return;
            }

            RankingState = state;
            ReactiveRankingState.Initialize(RankingState);
        }

        /// <summary>
        /// 샵 상태를 할당한다.
        /// </summary>
        /// <param name="state"></param>
        public void SetShopState(ShopState state)
        {
            if (state is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(SetShopState)}] {nameof(state)} is null.");
                return;
            }

            ShopState = state;
            ReactiveShopState.Initialize(ShopState);
        }

        public void SetWeeklyArenaState(WeeklyArenaState state)
        {
            if (state is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(SetWeeklyArenaState)}] {nameof(state)} is null.");
                return;
            }

            LocalStateSettings.Instance.InitializeWeeklyArena(state);
            WeeklyArenaState = LocalStateSettings.Instance.Modify(state);
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

            LocalStateSettings.Instance.InitializeAgentAndAvatars(state);
            AgentState = LocalStateSettings.Instance.Modify(state);
            ReactiveAgentState.Initialize(AgentState);

            if (!getAllOfAvatarStates)
                return;

            foreach (var pair in AgentState.avatarAddresses)
            {
                AddOrReplaceAvatarState(pair.Value, pair.Key);
            }
        }

        public AvatarState AddOrReplaceAvatarState(Address avatarAddress, int index, bool initializeReactiveState = true)
        {
            var avatarState =
                new AvatarState((Bencodex.Types.Dictionary) Game.Game.instance.Agent.GetState(avatarAddress));
            return AddOrReplaceAvatarState(avatarState, index, initializeReactiveState);
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

            state = LocalStateSettings.Instance.Modify(state);

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
                throw new KeyNotFoundException($"{nameof(index)}({index})");

            CurrentAvatarKey = index;
            UpdateCurrentAvatarState(_avatarStates[CurrentAvatarKey], initializeReactiveState);
            return CurrentAvatarState;
        }

        /// <summary>
        /// 아바타 상태 선택을 해지한다.
        /// </summary>
        public void DeselectAvatar()
        {
            CurrentAvatarKey = -1;
            UpdateCurrentAvatarState(null);
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
