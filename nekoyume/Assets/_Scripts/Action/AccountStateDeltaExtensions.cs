using System;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.Action
{
    public static class AccountStateDeltaExtensions
    {
        public static bool TryGetState<T>(this IAccountStateDelta states, Address address, out T result)
            where T : IValue
        {
            IValue raw = states.GetState(address);
            if (raw is T v)
            {
                result = v;
                return true;
            }

            Debug.LogErrorFormat(
                "Expected a {0}, but got invalid state ({1}): ({2}) {3}",
                typeof(T).Name,
                address.ToHex(),
                raw?.GetType().Name,
                raw
            );
            result = default;
            return false;
        }

        public static AgentState GetAgentState(this IAccountStateDelta states, Address address)
        {
            var serializedAgent = states.GetState(address);
            if (serializedAgent is null)
            {
                Debug.LogWarningFormat("No agent state ({0})", address.ToHex());
                return null;
            }

            try
            {
                return new AgentState((Bencodex.Types.Dictionary) serializedAgent);
            }
            catch (InvalidCastException e)
            {
                Debug.LogErrorFormat(
                    "Invalid agent state ({0}): {1}",
                    address.ToHex(),
                    serializedAgent
                );
                Debug.LogException(e);
                return null;
            }
        }

        public static AvatarState GetAvatarState(this IAccountStateDelta states, Address address)
        {
            var serializedAvatar = states.GetState(address);
            if (serializedAvatar is null)
            {
                Debug.LogWarningFormat("No avatar state ({0})", address.ToHex());
                return null;
            }

            try
            {
                return new AvatarState((Bencodex.Types.Dictionary) serializedAvatar);
            }
            catch (InvalidCastException e)
            {
                Debug.LogErrorFormat(
                    "Invalid avatar state ({0}): {1}",
                    address.ToHex(),
                    serializedAvatar
                );
                Debug.LogException(e);
                return null;
            }
        }

        public static bool TryGetAgentAvatarStates(
            this IAccountStateDelta states,
            Address agentAddress,
            Address avatarAddress,
            out AgentState agentState,
            out AvatarState avatarState
        )
        {
            avatarState = null;
            agentState = states.GetAgentState(agentAddress);
            if (agentState is null)
            {
                return false;
            }
            if (!agentState.avatarAddresses.ContainsValue(avatarAddress))
            {
                Debug.LogErrorFormat(
                    "The avatar {0} does not belong to the agent {1}.",
                    avatarAddress.ToHex(),
                    agentAddress.ToHex()
                );
                return false;
            }

            avatarState = states.GetAvatarState(avatarAddress);
            return !(avatarState is null);
        }

        public static WeeklyArenaState GetWeeklyArenaState(this IAccountStateDelta states, Address address)
        {
            var iValue = states.GetState(address);
            if (iValue is null)
            {
                Debug.LogWarningFormat("No weekly arena state ({0})", address.ToHex());
                return null;
            }

            try
            {
                return new WeeklyArenaState(iValue);
            }
            catch (InvalidCastException e)
            {
                Debug.LogErrorFormat(
                    "Invalid weekly arena state ({0}): {1}",
                    address.ToHex(),
                    iValue
                );
                Debug.LogException(e);
                return null;
            }
        }
    }
}
