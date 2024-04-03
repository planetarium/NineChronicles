using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.State.Modifiers;
using UnityEngine;

namespace Nekoyume.State
{
    // todo: 기간이 지난 주간 랭킹 상태 변경자들을 자동으로 클리어해줘야 함.
    /// <summary>
    /// 체인이 포함하는 특정 상태에 대한 상태 변경자를 관리한다.
    /// 모든 상태 변경자는 대상 상태의 체인 내 주소를 기준으로 분류한다.
    /// </summary>
    public class LocalLayer
    {
        /// <summary>
        /// 변경자 정보는 대상 주소(Address), 상태 변경자(Modifiers)로 구성된다.
        /// </summary>
        /// <typeparam name="T">AgentStateModifier 등</typeparam>
        private class ModifierInfo<T> where T : class
        {
            public readonly Address Address;
            public readonly List<T> Modifiers;

            public ModifierInfo(Address address)
            {
                Address = address;
                Modifiers = new List<T>();
            }
        }

        public static LocalLayer Instance => Game.Game.instance.LocalLayer;

        private ModifierInfo<AgentStateModifier> _agentModifierInfo;

        private ModifierInfo<AgentGoldModifier> _agentGoldModifierInfo;

        private ModifierInfo<AgentCrystalModifier> _agentCrystalModifierInfo;

        private ModifierInfo<AvatarStateModifier> _avatarModifierInfo;

        private readonly Dictionary<Address, ModifierInfo<CombinationSlotStateModifier>>
            _combinationSlotModifierInfos = new();

        private ModifierInfo<WeeklyArenaStateModifier> _weeklyArenaModifierInfo;

        #region Initialization

        /// <summary>
        /// 인자로 받은 에이전트 상태를 바탕으로 로컬 세팅을 초기화 한다.
        /// 에이전트 상태가 포함하는 모든 아바타 상태 또한 포함된다.
        /// 이미 초기화되어 있는 에이전트와 같을 경우에는 아바타의 주소에 대해서만
        /// </summary>
        /// <param name="agentState"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void InitializeAgentAndAvatars(AgentState agentState)
        {
            if (agentState is null)
            {
                throw new ArgumentNullException(nameof(agentState));
            }

            var address = agentState.address;
            // 이미 초기화되어 있는 에이전트와 같을 경우.
            if (!(_agentModifierInfo is null) &&
                _agentModifierInfo.Address.Equals(address))
            {
                return;
            }

            // _agentModifierInfo 초기화하기.
            _agentModifierInfo =
                new ModifierInfo<AgentStateModifier>(address);
            _agentGoldModifierInfo = new ModifierInfo<AgentGoldModifier>(address);
            _agentCrystalModifierInfo = new ModifierInfo<AgentCrystalModifier>(address);
        }

        public void InitializeCurrentAvatarState(AvatarState avatarState)
        {
            if (avatarState is null)
            {
                _avatarModifierInfo = null;
                return;
            }

            var address = avatarState.address;
            if (!(_avatarModifierInfo is null) &&
                _avatarModifierInfo.Address.Equals(address))
            {
                return;
            }

            _avatarModifierInfo = new ModifierInfo<AvatarStateModifier>(address);
        }

        public void InitializeCombinationSlotsByCurrentAvatarState(AvatarState avatarState)
        {
            if (avatarState is null)
            {
                _combinationSlotModifierInfos.Clear();
                return;
            }

            foreach (var address in avatarState.combinationSlotAddresses.Where(address =>
                !_combinationSlotModifierInfos.ContainsKey(address)))
            {
                _combinationSlotModifierInfos.Add(
                    address,
                    new ModifierInfo<CombinationSlotStateModifier>(address));
            }
        }

        public void InitializeWeeklyArena(WeeklyArenaState weeklyArenaState)
        {
            var address = weeklyArenaState.address;
            _weeklyArenaModifierInfo = new ModifierInfo<WeeklyArenaStateModifier>(address);
        }

        #endregion

        #region Set & Reset

        /// <summary>
        /// 인자로 받은 워크샵 슬롯에 대한 상태 변경자를 적용합니다.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="modifier"></param>
        public void Set(Address address, CombinationSlotStateModifier modifier)
        {
            if (!_combinationSlotModifierInfos.ContainsKey(address))
            {
                return;
            }

            var modifierInfo = _combinationSlotModifierInfos[address];
            if (!address.Equals(modifierInfo.Address))
            {
                return;
            }

            var modifiers = modifierInfo.Modifiers;
            if (TryGetSameTypeModifier(modifier, modifiers, out var outModifier))
            {
                modifiers.Remove(outModifier);
            }

            modifiers.Add(modifier);
        }

        public void ResetCombinationSlotModifiers<T>(Address address)
            where T : CombinationSlotStateModifier
        {
            if (!_combinationSlotModifierInfos.ContainsKey(address))
            {
                return;
            }

            var modifierInfo = _combinationSlotModifierInfos[address];
            if (!address.Equals(modifierInfo.Address))
            {
                return;
            }

            var modifiers = modifierInfo.Modifiers;
            if (TryGetSameTypeModifier(typeof(T), modifiers, out var outModifier))
            {
                modifiers.Remove(outModifier);
            }
        }

        #endregion

        #region Add

        /// <summary>
        /// 인자로 받은 에이전트에 대한 상태 변경자를 더한다.
        /// </summary>
        /// <param name="agentAddress"></param>
        /// <param name="modifier"></param>
        public void Add(Address agentAddress, AgentStateModifier modifier)
        {
            // FIXME: 다른 Add() 오버로드와 겹치는 로직이 아주 많음.
            if (modifier is null ||
                modifier.IsEmpty)
            {
                return;
            }

            var modifierInfo = _agentModifierInfo;
            if (agentAddress.Equals(modifierInfo.Address))
            {
                var modifiers = modifierInfo.Modifiers;
                if (TryGetSameTypeModifier(modifier, modifiers, out var outModifier))
                {
                    outModifier.Add(modifier);
                    if (outModifier.IsEmpty)
                    {
                        modifiers.Remove(outModifier);
                    }
                }
                else
                {
                    modifiers.Add(modifier);
                }
            }
        }

        public void Add(Address agentAddress, AgentGoldModifier modifier)
        {
            // FIXME: 다른 Add() 오버로드와 겹치는 로직이 아주 많음.
            if (modifier is null || modifier.IsEmpty)
            {
                return;
            }

            if (agentAddress.Equals(_agentGoldModifierInfo.Address))
            {
                var modifiers = _agentGoldModifierInfo.Modifiers;
                if (TryGetSameTypeModifier(modifier, modifiers, out var outModifier))
                {
                    outModifier.Add(modifier);
                    if (outModifier.IsEmpty)
                    {
                        modifiers.Remove(outModifier);
                    }
                }
                else
                {
                    modifiers.Add(modifier);
                }
            }
        }

        public void Add(Address agentAddress, AgentCrystalModifier modifier)
        {
            if (modifier is null || modifier.IsEmpty)
            {
                return;
            }

            if (agentAddress.Equals(_agentCrystalModifierInfo.Address))
            {
                var modifiers = _agentCrystalModifierInfo.Modifiers;
                if (TryGetSameTypeModifier(modifier, modifiers, out var outModifier))
                {
                    outModifier.Add(modifier);
                    if (outModifier.IsEmpty)
                    {
                        modifiers.Remove(outModifier);
                    }
                }
                else
                {
                    modifiers.Add(modifier);
                }
            }
        }

        /// <summary>
        /// 인자로 받은 아바타에 대한 상태 변경자를 더한다.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="modifier"></param>
        public void Add(Address avatarAddress, AvatarStateModifier modifier)
        {
            // FIXME: 다른 Add() 오버로드와 겹치는 로직이 아주 많음.
            if (modifier is null ||
                modifier.IsEmpty)
            {
                return;
            }

            var modifierInfo = _avatarModifierInfo?.Address.Equals(avatarAddress) ?? false
                ? _avatarModifierInfo
                : null;

            if (!(modifierInfo is null))
            {
                var modifiers = modifierInfo.Modifiers;
                if (TryGetSameTypeModifier(modifier, modifiers, out var outModifier))
                {
                    outModifier.Add(modifier);
                    if (outModifier.IsEmpty)
                    {
                        modifiers.Remove(outModifier);
                    }
                }
                else
                {
                    modifiers.Add(modifier);
                }
            }
        }

        /// <summary>
        /// 인자로 받은 주간 아레나에 대한 상태 변경자를 더한다.
        /// </summary>
        /// <param name="weeklyArenaAddress"></param>
        /// <param name="modifier"></param>
        public void Add(Address weeklyArenaAddress, WeeklyArenaStateModifier modifier)
        {
            // FIXME: 다른 Add() 오버로드와 겹치는 로직이 아주 많음.
            if (modifier is null ||
                modifier.IsEmpty)
            {
                return;
            }

            var modifierInfo = _weeklyArenaModifierInfo;
            if (weeklyArenaAddress.Equals(modifierInfo.Address))
            {
                var modifiers = modifierInfo.Modifiers;
                if (TryGetSameTypeModifier(modifier, modifiers, out var outModifier))
                {
                    outModifier.Add(modifier);
                    if (outModifier.IsEmpty)
                    {
                        modifiers.Remove(outModifier);
                    }
                }
                else
                {
                    modifiers.Add(modifier);
                }
            }
        }

        #endregion

        #region Remove

        /// <summary>
        /// 인자로 받은 에이전트에 대한 상태 변경자를 뺀다.
        /// </summary>
        /// <param name="agentAddress"></param>
        /// <param name="modifier"></param>
        public void Remove(Address agentAddress, AgentStateModifier modifier)
        {
            if (modifier is null ||
                modifier.IsEmpty)
            {
                return;
            }

            var modifierInfo = _agentModifierInfo;
            if (agentAddress.Equals(modifierInfo.Address))
            {
                var modifiers = modifierInfo.Modifiers;
                if (TryGetSameTypeModifier(modifier, modifiers, out var outModifier))
                {
                    outModifier.Remove(modifier);
                    if (outModifier.IsEmpty)
                    {
                        modifiers.Remove(outModifier);
                    }

                    modifier = outModifier;
                }
                else
                {
                    modifier = null;
                }
            }

            if (modifier is null)
            {
                NcDebug.LogWarning(
                    $"[{nameof(LocalLayer)}] No found {nameof(modifier)} of {nameof(agentAddress)}");
            }
        }

        /// <summary>
        /// 인자로 받은 아바타에 대한 상태 변경자를 뺀다.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="modifier"></param>
        public void Remove(Address avatarAddress, AvatarStateModifier modifier)
        {
            if (modifier is null ||
                modifier.IsEmpty)
            {
                return;
            }

            var modifierInfo = _avatarModifierInfo?.Address.Equals(avatarAddress) ?? false
                ? _avatarModifierInfo
                : null;

            if (!(modifierInfo is null))
            {
                var modifiers = modifierInfo.Modifiers;
                if (TryGetSameTypeModifier(modifier, modifiers, out var outModifier))
                {
                    outModifier.Remove(modifier);
                    if (outModifier.IsEmpty)
                    {
                        modifiers.Remove(outModifier);
                    }

                    modifier = outModifier;
                }
                else
                {
                    modifier = null;
                }
            }

            if (modifier is null)
            {
                NcDebug.LogWarning(
                    $"[{nameof(LocalLayer)}] No found {nameof(modifier)} of {nameof(avatarAddress)}");
            }
        }

        /// <summary>
        /// 인자로 받은 주간 아레나에 대한 상태 변경자를 뺀다.
        /// </summary>
        /// <param name="weeklyArenaAddress"></param>
        /// <param name="modifier"></param>
        public void Remove(Address weeklyArenaAddress, WeeklyArenaStateModifier modifier)
        {
            if (modifier is null ||
                modifier.IsEmpty)
            {
                return;
            }

            var modifierInfo = _weeklyArenaModifierInfo;
            if (weeklyArenaAddress.Equals(modifierInfo.Address))
            {
                var modifiers = modifierInfo.Modifiers;
                if (TryGetSameTypeModifier(modifier, modifiers, out var outModifier))
                {
                    outModifier.Remove(modifier);
                    if (outModifier.IsEmpty)
                    {
                        modifiers.Remove(outModifier);
                    }

                    modifier = outModifier;
                }
                else
                {
                    modifier = null;
                }
            }

            if (modifier is null)
            {
                NcDebug.LogWarning(
                    $"[{nameof(LocalLayer)}] No found {nameof(modifier)} of {nameof(weeklyArenaAddress)}");
            }
        }

        #endregion

        #region Modify

        /// <summary>
        /// 인자로 받은 에이전트 상태에 로컬 세팅을 반영한다.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public AgentState Modify(AgentState state)
        {
            if (state is null ||
                !state.address.Equals(_agentModifierInfo.Address))
            {
                return state;
            }

            return PostModify(state, _agentModifierInfo);
        }

        public FungibleAssetValue ModifyCrystal(FungibleAssetValue value)
        {
            if (value.Equals(default) ||
                !value.Currency.Equals(CrystalCalculator.CRYSTAL) ||
                value.Sign == 0)
            {
                return value;
            }

            return PostModifyValue(value, _agentCrystalModifierInfo);
        }

        /// <summary>
        /// 인자로 받은 잔고 상태에 로컬 세팅을 반영한다.
        /// </summary>
        public GoldBalanceState Modify(GoldBalanceState state)
        {
            if (state is null ||
                !state.address.Equals(_agentGoldModifierInfo.Address))
            {
                return state;
            }

            return PostModify(state, _agentGoldModifierInfo);
        }

        /// <summary>
        /// 인자로 받은 아바타 상태에 로컬 세팅을 반영한다.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public AvatarState Modify(AvatarState state)
        {
            if (state is null ||
                _avatarModifierInfo is null)
            {
                return state;
            }

            var address = state.address;
            if (!_avatarModifierInfo.Address.Equals(address))
            {
                return state;
            }

            return PostModify(state, _avatarModifierInfo);
        }

        public AvatarState ModifyInventoryOnly(AvatarState avatarState)
        {
            if (avatarState is null ||
                _avatarModifierInfo is null)
            {
                return null;
            }

            var address = avatarState.address;
            if (!_avatarModifierInfo.Address.Equals(address))
            {
                return null;
            }

            foreach (var m in _avatarModifierInfo.Modifiers)
            {
                if (m is not AvatarInventoryFungibleItemRemover &&
                    m is not AvatarInventoryItemEquippedModifier &&
                    m is not AvatarInventoryTradableItemRemover &&
                    m is not AvatarInventoryNonFungibleItemRemover)
                {
                    continue;
                }

                avatarState = m.Modify(avatarState);
            }

            return avatarState;
        }

        public CombinationSlotState Modify(CombinationSlotState state)
        {
            if (state is null)
            {
                return null;
            }

            var address = state.address;
            if (!_combinationSlotModifierInfos.ContainsKey(address))
            {
                return state;
            }

            var modifierInfo = _combinationSlotModifierInfos[address];
            return modifierInfo is null
                ? state
                : PostModify(state, modifierInfo);
        }

        /// <summary>
        /// 인자로 받은 주간 아레나 상태에 로컬 세팅을 반영한다.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public WeeklyArenaState Modify(WeeklyArenaState state)
        {
            if (state is null ||
                !state.address.Equals(_weeklyArenaModifierInfo.Address))
            {
                return null;
            }

            return PostModify(state, _weeklyArenaModifierInfo);
        }

        private static TState PostModify<TState, TModifier>(
            TState state,
            ModifierInfo<TModifier> modifierInfo)
            where TState : Model.State.State
            where TModifier : class, IStateModifier<TState>
        {
            foreach (var modifier in modifierInfo.Modifiers)
            {
                state = modifier.Modify(state);
            }

            return state;
        }

        private static TValue PostModifyValue<TValue, TModifier>(
            TValue value,
            ModifierInfo<TModifier> modifierInfo)
            where TValue : struct
            where TModifier : class, IValueModifier<TValue>
        {
            foreach (var modifier in modifierInfo.Modifiers)
            {
                value = modifier.Modify(value);
            }

            return value;
        }

        #endregion

        #region Clear

        /// <summary>
        /// `T`형 상태 변경자를 모두 제거한다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void ClearAvatarModifiers<T>(Address avatarAddress)
            where T : AvatarStateModifier
        {
            if (_avatarModifierInfo is null ||
                !_avatarModifierInfo.Address.Equals(avatarAddress))
            {
                return;
            }

            while (true)
            {
                if (!TryGetSameTypeModifier(
                    typeof(T),
                    _avatarModifierInfo.Modifiers,
                    out var modifier))
                {
                    break;
                }

                _avatarModifierInfo.Modifiers.Remove(modifier);
            }
        }

        #endregion

        /// <summary>
        /// `modifiers`가 `modifier`와 같은 타입의 객체를 포함하고 있다면, 그것을 반환한다.
        /// </summary>
        /// <param name="modifier"></param>
        /// <param name="modifiers"></param>
        /// <param name="outModifier"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static bool TryGetSameTypeModifier<T>(
            T modifier,
            IEnumerable<T> modifiers,
            out T outModifier)
        {
            return TryGetSameTypeModifier(
                modifier.GetType(),
                modifiers,
                out outModifier);
        }

        private static bool TryGetSameTypeModifier<T>(
            Type type,
            IEnumerable<T> modifiers,
            out T outModifier)
        {
            try
            {
                outModifier = modifiers.First(e => e.GetType() == type);
                return true;
            }
            catch
            {
                outModifier = default;
                return false;
            }
        }
    }
}
