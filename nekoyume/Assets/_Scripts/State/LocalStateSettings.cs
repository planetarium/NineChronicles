using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Libplanet;
using Nekoyume.Model.State;
using Nekoyume.State.Modifiers;
using UnityEngine;

namespace Nekoyume.State
{
    // todo: 기간이 지난 주간 랭킹 상태 변경자들을 자동으로 클리어해줘야 함.
    /// <summary>
    /// 체인이 포함하는 특정 상태에 대한 상태 변경자를 관리한다.
    /// 모든 상태 변경자는 대상 상태의 체인 내 주소를 기준으로 분류한다.
    /// 상태 변경자는 휘발성과 비휘발성으로 구분해서 다룬다.(volatile, nonVolatile)
    /// 휘발성은 에이전트 혹은 아바타의 주소가 새롭게 설정되는 경우 사라진다.
    /// 비휘발성은 PlayerPrefs에 저장한다.
    /// </summary>
    public class LocalStateSettings
    {
        /// <summary>
        /// 변경자 정보는 대상 주소(Address), 비휘발성 상태 변경자(NonVolatileModifiers), 휘발성 상태 변경자(VolatileModifiers)로 구성된다.
        /// </summary>
        /// <typeparam name="T">AgentStateModifier, AvatarStateModifier</typeparam>
        private class ModifierInfo<T>
        {
            public readonly Address Address;
            public readonly List<T> NonVolatileModifiers;
            public readonly List<T> VolatileModifiers;

            public ModifierInfo(Address address, List<T> nonVolatileModifiers)
            {
                Address = address;
                NonVolatileModifiers = nonVolatileModifiers;
                VolatileModifiers = new List<T>();
            }
        }

        public static LocalStateSettings Instance => Game.Game.instance.LocalStateSettings;

        private ModifierInfo<AgentStateModifier> _agentModifierInfo;

        private readonly List<ModifierInfo<AvatarStateModifier>> _avatarModifierInfos =
            new List<ModifierInfo<AvatarStateModifier>>();

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
                throw new ArgumentNullException(nameof(agentState));

            var agentAddress = agentState.address;
            // 이미 초기화되어 있는 에이전트와 같을 경우.
            if (!(_agentModifierInfo is null) &&
                _agentModifierInfo.Address.Equals(agentAddress))
            {
                // _avatarModifierInfos에는 있지만, 에이전트에는 없는 것 삭제하기.
                foreach (var info in _avatarModifierInfos
                    .Where(info =>
                        !agentState.avatarAddresses.Values.Any(avatarAddress => avatarAddress.Equals(info.Address)))
                    .ToList())
                {
                    _avatarModifierInfos.Remove(info);
                }

                // 에이전트에는 있지만, _avatarModifierInfos에는 없는 것 추가하기. 
                foreach (var avatarAddress in agentState.avatarAddresses.Values
                    .Where(avatarAddress =>
                        !_avatarModifierInfos.Any(
                            avatarModifierInfo => avatarAddress.Equals(avatarModifierInfo.Address))))
                {
                    _avatarModifierInfos.Add(new ModifierInfo<AvatarStateModifier>(avatarAddress,
                        LoadModifiers<AvatarStateModifier>(avatarAddress)));
                }

                return;
            }

            // _agentModifierInfo 초기화하기.
            _agentModifierInfo =
                new ModifierInfo<AgentStateModifier>(agentAddress, LoadModifiers<AgentStateModifier>(agentAddress));
            foreach (var avatarAddress in agentState.avatarAddresses.Values)
            {
                _avatarModifierInfos.Add(new ModifierInfo<AvatarStateModifier>(avatarAddress,
                    LoadModifiers<AvatarStateModifier>(avatarAddress)));
            }
        }

        public void InitializeWeeklyArena(WeeklyArenaState weeklyArenaState)
        {
            var address = weeklyArenaState.address;
            _weeklyArenaModifierInfo =
                new ModifierInfo<WeeklyArenaStateModifier>(address, LoadModifiers<WeeklyArenaStateModifier>(address));
        }

        #endregion

        #region Add

        /// <summary>
        /// 인자로 받은 에이전트에 대한 상태 변경자를 더한다.
        /// </summary>
        /// <param name="agentAddress"></param>
        /// <param name="modifier"></param>
        /// <param name="isVolatile"></param>
        public void Add(Address agentAddress, AgentStateModifier modifier, bool isVolatile = false)
        {
            if (modifier is null ||
                modifier.IsEmpty)
                return;

            var modifierInfo = _agentModifierInfo;
            if (agentAddress.Equals(modifierInfo.Address))
            {
                var modifiers = isVolatile
                    ? modifierInfo.VolatileModifiers
                    : modifierInfo.NonVolatileModifiers;
                if (TryGetSameTypeModifier(modifier, modifiers, out var outModifier))
                {
                    outModifier.Add(modifier);
                    if (outModifier.IsEmpty)
                    {
                        modifiers.Remove(outModifier);
                    }

                    modifier = outModifier;
                }
                else
                {
                    modifiers.Add(modifier);
                }
            }
            else if (!isVolatile)
            {
                if (TryLoadModifier<AgentStateModifier>(agentAddress, modifier.GetType(), out var outModifier))
                {
                    outModifier.Add(modifier);
                    modifier = outModifier;
                }
            }

            PostAdd(agentAddress, modifier, isVolatile);
        }

        /// <summary>
        /// 인자로 받은 아바타에 대한 상태 변경자를 더한다.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="modifier"></param>
        /// <param name="isVolatile"></param>
        public void Add(Address avatarAddress, AvatarStateModifier modifier, bool isVolatile = false)
        {
            if (modifier is null ||
                modifier.IsEmpty)
                return;

            var modifierInfo = _avatarModifierInfos.FirstOrDefault(e => e.Address.Equals(avatarAddress));
            if (!(modifierInfo is null))
            {
                var modifiers = isVolatile
                    ? modifierInfo.VolatileModifiers
                    : modifierInfo.NonVolatileModifiers;
                if (TryGetSameTypeModifier(modifier, modifiers, out var outModifier))
                {
                    outModifier.Add(modifier);
                    if (outModifier.IsEmpty)
                    {
                        modifiers.Remove(outModifier);
                    }

                    modifier = outModifier;
                }
                else
                {
                    modifiers.Add(modifier);
                }
            }
            else if (!isVolatile)
            {
                if (TryLoadModifier<AvatarStateModifier>(avatarAddress, modifier.GetType(), out var outModifier))
                {
                    outModifier.Add(modifier);
                    modifier = outModifier;
                }
            }

            PostAdd(avatarAddress, modifier, isVolatile);
        }

        /// <summary>
        /// 인자로 받은 주간 아레나에 대한 상태 변경자를 더한다.
        /// </summary>
        /// <param name="weeklyArenaAddress"></param>
        /// <param name="modifier"></param>
        /// <param name="isVolatile"></param>
        public void Add(Address weeklyArenaAddress, WeeklyArenaStateModifier modifier, bool isVolatile = false)
        {
            if (modifier is null ||
                modifier.IsEmpty)
                return;

            var modifierInfo = _weeklyArenaModifierInfo;
            if (weeklyArenaAddress.Equals(modifierInfo.Address))
            {
                var modifiers = isVolatile
                    ? modifierInfo.VolatileModifiers
                    : modifierInfo.NonVolatileModifiers;
                if (TryGetSameTypeModifier(modifier, modifiers, out var outModifier))
                {
                    outModifier.Add(modifier);
                    if (outModifier.IsEmpty)
                    {
                        modifiers.Remove(outModifier);
                    }

                    modifier = outModifier;
                }
                else
                {
                    modifiers.Add(modifier);
                }
            }
            else if (!isVolatile)
            {
                if (TryLoadModifier<WeeklyArenaStateModifier>(weeklyArenaAddress, modifier.GetType(),
                    out var outModifier))
                {
                    outModifier.Add(modifier);
                    modifier = outModifier;
                }
            }

            PostAdd(weeklyArenaAddress, modifier, isVolatile);
        }

        /// <summary>
        /// `Add` 메서드 이후에 호출한다.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="modifier"></param>
        /// <param name="isVolatile"></param>
        /// <typeparam name="T"></typeparam>
        private static void PostAdd<T>(Address address, IAccumulatableStateModifier<T> modifier, bool isVolatile)
            where T : Model.State.State
        {
            if (isVolatile)
                return;

            SaveModifier(address, modifier);
        }

        #endregion

        #region Remove

        /// <summary>
        /// 인자로 받은 에이전트에 대한 상태 변경자를 뺀다.
        /// </summary>
        /// <param name="agentAddress"></param>
        /// <param name="modifier"></param>
        /// <param name="isVolatile"></param>
        public void Remove(Address agentAddress, AgentStateModifier modifier, bool isVolatile = false)
        {
            if (modifier is null ||
                modifier.IsEmpty)
                return;

            var modifierInfo = _agentModifierInfo;
            if (agentAddress.Equals(modifierInfo.Address))
            {
                var modifiers = isVolatile
                    ? modifierInfo.VolatileModifiers
                    : modifierInfo.NonVolatileModifiers;
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
            else if (!isVolatile)
            {
                if (TryLoadModifier<AgentStateModifier>(agentAddress, modifier.GetType(), out var outModifier))
                {
                    outModifier.Remove(modifier);
                    modifier = outModifier;
                }
                else
                {
                    modifier = null;
                }
            }

            if (modifier is null)
            {
                Debug.LogWarning(
                    $"[{nameof(LocalStateSettings)}] No found {nameof(modifier)} of {nameof(agentAddress)}");

                return;
            }

            PostRemove(agentAddress, modifier, isVolatile);
        }

        /// <summary>
        /// 인자로 받은 아바타에 대한 상태 변경자를 뺀다.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="modifier"></param>
        /// <param name="isVolatile"></param>
        public void Remove(Address avatarAddress, AvatarStateModifier modifier, bool isVolatile = false)
        {
            if (modifier is null ||
                modifier.IsEmpty)
                return;

            var modifierInfo = _avatarModifierInfos.FirstOrDefault(e => e.Address.Equals(avatarAddress));
            if (!(modifierInfo is null))
            {
                var modifiers = isVolatile
                    ? modifierInfo.VolatileModifiers
                    : modifierInfo.NonVolatileModifiers;
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
            else if (!isVolatile)
            {
                if (TryLoadModifier<AvatarStateModifier>(avatarAddress, modifier.GetType(), out var outModifier))
                {
                    outModifier.Remove(modifier);
                    modifier = outModifier;
                }
                else
                {
                    modifier = null;
                }
            }

            if (modifier is null)
            {
                Debug.LogWarning(
                    $"[{nameof(LocalStateSettings)}] No found {nameof(modifier)} of {nameof(avatarAddress)}");

                return;
            }

            PostRemove(avatarAddress, modifier, isVolatile);
        }

        /// <summary>
        /// 인자로 받은 주간 아레나에 대한 상태 변경자를 뺀다.
        /// </summary>
        /// <param name="weeklyArenaAddress"></param>
        /// <param name="modifier"></param>
        /// <param name="isVolatile"></param>
        public void Remove(Address weeklyArenaAddress, WeeklyArenaStateModifier modifier, bool isVolatile = false)
        {
            if (modifier is null ||
                modifier.IsEmpty)
                return;

            var modifierInfo = _weeklyArenaModifierInfo;
            if (weeklyArenaAddress.Equals(modifierInfo.Address))
            {
                var modifiers = isVolatile
                    ? modifierInfo.VolatileModifiers
                    : modifierInfo.NonVolatileModifiers;
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
            else if (!isVolatile)
            {
                if (TryLoadModifier<WeeklyArenaStateModifier>(weeklyArenaAddress, modifier.GetType(),
                    out var outModifier))
                {
                    outModifier.Remove(modifier);
                    modifier = outModifier;
                }
                else
                {
                    modifier = null;
                }
            }

            if (modifier is null)
            {
                Debug.LogWarning(
                    $"[{nameof(LocalStateSettings)}] No found {nameof(modifier)} of {nameof(weeklyArenaAddress)}");

                return;
            }

            PostRemove(weeklyArenaAddress, modifier, isVolatile);
        }

        /// <summary>
        /// `Remove` 메서드 이후에 호출한다.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="modifier"></param>
        /// <param name="isVolatile"></param>
        /// <typeparam name="T"></typeparam>
        private static void PostRemove<T>(Address address, IAccumulatableStateModifier<T> modifier, bool isVolatile)
            where T : Model.State.State
        {
            if (isVolatile)
                return;

            SaveModifier(address, modifier);
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
                return state;

            return PostModify(state, _agentModifierInfo);

            // foreach (var modifier in _agentModifierInfo.NonVolatileModifiers)
            // {
            //     modifier.Modify(state);
            // }
            //
            // foreach (var modifier in _agentModifierInfo.VolatileModifiers)
            // {
            //     modifier.Modify(state);
            // }
            //
            // return state;
        }

        /// <summary>
        /// 인자로 받은 아바타 상태에 로컬 세팅을 반영한다.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public AvatarState Modify(AvatarState state)
        {
            if (state is null)
                return null;

            var modifierInfo = _avatarModifierInfos.FirstOrDefault(e => e.Address.Equals(state.address));
            return modifierInfo is null
                ? state
                : PostModify(state, modifierInfo);

            // foreach (var modifier in info.NonVolatileModifiers)
            // {
            //     modifier.Modify(state);
            // }
            //
            // foreach (var modifier in info.VolatileModifiers)
            // {
            //     modifier.Modify(state);
            // }
            //
            // return state;
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
                return null;

            return PostModify(state, _weeklyArenaModifierInfo);
        }

        private static TState PostModify<TState, TModifier>(TState state, ModifierInfo<TModifier> modifierInfo)
            where TState : Model.State.State where TModifier : IStateModifier<TState>
        {
            foreach (var modifier in modifierInfo.NonVolatileModifiers)
            {
                modifier.Modify(state);
            }

            foreach (var modifier in modifierInfo.VolatileModifiers)
            {
                modifier.Modify(state);
            }

            return state;
        }

        #endregion

        #region Save & Load

        /// <summary>
        /// 인자로 받은 상태 변경자를 `PlayerPrefs`에 저장한다.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="modifier"></param>
        /// <typeparam name="T"></typeparam>
        private static void SaveModifier<T>(Address address, IAccumulatableStateModifier<T> modifier) where T : Model.State.State
        {
            var key = GetKey(address, modifier);
            if (modifier.IsEmpty)
            {
                PlayerPrefs.DeleteKey(key);
                return;
            }

            var json = JsonUtility.ToJson(modifier);
            PlayerPrefs.SetString(key, json);
        }

        private static List<T> LoadModifiers<T>(Address address) where T : class
        {
            var baseType = typeof(T);
            var modifiers = new List<T>();

            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(e => e.IsInheritsFrom(baseType));
            foreach (var type in types)
            {
                if (!TryLoadModifier<T>(address, type, out var outModifier))
                    continue;

                modifiers.Add(outModifier);
            }

            return modifiers;
        }

        /// <summary>
        /// `address`와 `typeOfModifier`에 해당하는 `T`형 상태 변경자를 반환한다. 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="typeOfModifier"></param>
        /// <param name="outModifier"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static bool TryLoadModifier<T>(Address address, Type typeOfModifier,
            out T outModifier) where T : class
        {
            var key = GetKey(address, typeOfModifier);
            if (!PlayerPrefs.HasKey(key))
            {
                outModifier = null;
                return false;
            }

            var json = PlayerPrefs.GetString(key);
            outModifier = (T) JsonUtility.FromJson(json, typeOfModifier);
            return !(outModifier is null);
        }

        #endregion

        #region Key

        /// <summary>
        /// 상태 변경자가 저장되는 키를 반환한다.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="modifier"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static string GetKey<T>(Address address, IAccumulatableStateModifier<T> modifier) where T : Model.State.State
        {
            return GetKey(address, modifier.GetType());
        }

        /// <summary>
        /// 상태 변경자가 저장되는 키를 반환한다.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="typeOfModifier"></param>
        /// <returns></returns>
        private static string GetKey(Address address, Type typeOfModifier)
        {
            var format = $"{nameof(LocalStateSettings)}_{{0}}_{{1}}";
            var name = typeOfModifier.Name;
            var key = string.Format(
                format,
                address.ToHex(),
                name);
            return key;
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
        private static bool TryGetSameTypeModifier<T>(T modifier,
            IEnumerable<T> modifiers, out T outModifier)
        {
            var type = modifier.GetType();
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
