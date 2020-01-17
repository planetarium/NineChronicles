using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Libplanet;
using Nekoyume.State.Modifiers;
using UnityEngine;

namespace Nekoyume.State
{
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

        /// <summary>
        /// 인자로 받은 에이전트 상태를 바탕으로 로컬 세팅을 초기화 한다.
        /// 에이전트 상태가 포함하는 모든 아바타 상태 또한 포함된다.
        /// 이미 초기화되어 있는 에이전트와 같을 경우에는 아바타의 주소에 대해서만 
        /// </summary>
        /// <param name="agentState"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Initialize(AgentState agentState)
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
                    .Where(info => !agentState.avatarAddresses.Values.Any(avatarAddress => avatarAddress.Equals(info.Address))))
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
                        LoadAvatarStateModifiers(avatarAddress)));
                }

                return;
            }

            // _agentModifierInfo 초기화하기.
            _agentModifierInfo =
                new ModifierInfo<AgentStateModifier>(agentAddress, LoadAgentStateModifiers(agentAddress));
            foreach (var avatarAddress in agentState.avatarAddresses.Values)
            {
                _avatarModifierInfos.Add(new ModifierInfo<AvatarStateModifier>(avatarAddress,
                    LoadAvatarStateModifiers(avatarAddress)));
            }
        }

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

            if (agentAddress.Equals(_agentModifierInfo.Address))
            {
                var modifiers = isVolatile
                    ? _agentModifierInfo.VolatileModifiers
                    : _agentModifierInfo.NonVolatileModifiers;
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

            if (isVolatile)
                return;

            SaveModifier(agentAddress, modifier);
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

            var info = _avatarModifierInfos.FirstOrDefault(e => e.Address.Equals(avatarAddress));
            if (!(info is null))
            {
                var modifiers = isVolatile ? info.VolatileModifiers : info.NonVolatileModifiers;
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
            
            if (isVolatile)
                return;

            SaveModifier(avatarAddress, modifier);
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

            if (agentAddress.Equals(_agentModifierInfo.Address))
            {
                var modifiers = isVolatile
                    ? _agentModifierInfo.VolatileModifiers
                    : _agentModifierInfo.NonVolatileModifiers;
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
            
            if (isVolatile)
                return;

            SaveModifier(agentAddress, modifier);
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

            var info = _avatarModifierInfos.FirstOrDefault(e => e.Address.Equals(avatarAddress));
            if (!(info is null))
            {
                var modifiers = isVolatile ? info.VolatileModifiers : info.NonVolatileModifiers;
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
            
            if (isVolatile)
                return;

            SaveModifier(avatarAddress, modifier);
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

            foreach (var modifier in _agentModifierInfo.NonVolatileModifiers)
            {
                modifier.Modify(state);
            }
            
            foreach (var modifier in _agentModifierInfo.VolatileModifiers)
            {
                modifier.Modify(state);
            }

            return state;
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

            var info = _avatarModifierInfos.FirstOrDefault(e => e.Address.Equals(state.address));
            if (info is null)
                return state;

            foreach (var modifier in info.NonVolatileModifiers)
            {
                modifier.Modify(state);
            }
            
            foreach (var modifier in info.VolatileModifiers)
            {
                modifier.Modify(state);
            }

            return state;
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
            IEnumerable<T> modifiers, out T outModifier) where T : class
        {
            var type = modifier.GetType();
            outModifier = modifiers.FirstOrDefault(e => e.GetType() == type);
            return !(outModifier is null);
        }

        /// <summary>
        /// 인자로 받은 상태 변경자를 `PlayerPrefs`에 저장한다.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="modifier"></param>
        /// <typeparam name="T"></typeparam>
        private static void SaveModifier<T>(Address address, IStateModifier<T> modifier) where T : State
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

        /// <summary>
        /// 인자로 받은 주소에 해당하는 에이전트 상태 변경자를 반환한다.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private static List<AgentStateModifier> LoadAgentStateModifiers(Address address)
        {
            var baseType = typeof(AgentStateModifier);
            var modifiers = new List<AgentStateModifier>();

            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(e => e.IsInheritsFrom(baseType));
            foreach (var type in types)
            {
                if (!TryLoadModifier<AgentStateModifier>(address, type, out var outModifier))
                    continue;

                modifiers.Add(outModifier);
            }

            return modifiers;
        }

        /// <summary>
        /// `address`에 해당하는 아바타 상태 변경자를 반환한다.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private static List<AvatarStateModifier> LoadAvatarStateModifiers(Address address)
        {
            var baseType = typeof(AvatarStateModifier);
            var modifiers = new List<AvatarStateModifier>();

            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(e => e.IsInheritsFrom(baseType));
            foreach (var type in types)
            {
                if (!TryLoadModifier<AvatarStateModifier>(address, type, out var outModifier))
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

        /// <summary>
        /// 상태 변경자가 저장되는 키를 반환한다.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="modifier"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static string GetKey<T>(Address address, IStateModifier<T> modifier) where T : State
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
            var key = string.Format(format, address.ToHex().Substring(0, 8), name);
            return key;
        }
    }
}
