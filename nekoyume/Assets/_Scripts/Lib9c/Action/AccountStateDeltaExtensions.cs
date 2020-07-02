using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Action
{
    public static class AccountStateDeltaExtensions
    {
        public static IAccountStateDelta MarkBalanceChanged(
            this IAccountStateDelta states,
            Currency currency,
            params Address[] accounts
        )
        {
            if (accounts.Length == 1)
            {
                return states.MintAsset(accounts[0], currency, 1);
            }
            else if (accounts.Length < 1)
            {
                return states;
            }

            for (int i = 1; i < accounts.Length; i++)
            {
                states = states.TransferAsset(accounts[i - 1], accounts[i], currency, 1, true);
            }

            return states;
        }

        public static bool TryGetState<T>(this IAccountStateDelta states, Address address, out T result)
            where T : IValue
        {
            IValue raw = states.GetState(address);
            if (raw is T v)
            {
                result = v;
                return true;
            }

            Log.Error(
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
                Log.Warning("No agent state ({0})", address.ToHex());
                return null;
            }

            try
            {
                return new AgentState((Bencodex.Types.Dictionary) serializedAgent);
            }
            catch (InvalidCastException e)
            {
                Log.Error(
                    e,
                    "Invalid agent state ({0}): {1}",
                    address.ToHex(),
                    serializedAgent
                );

                return null;
            }
        }

        public static bool TryGetGoldBalance(
            this IAccountStateDelta states,
            Address address,
            out BigInteger balance)
        {
            try
            {
                balance = states.GetBalance(address, Currencies.Gold);
                return true;
            }
            catch (BalanceDoesNotExistsException)
            {
                balance = default;
                return false;
            }
        }

        // FIXME NRE를 유발할 수 있는 위험한 방식. null 없이도 돌아가게 고쳐야 합니다.
        public static GoldBalanceState GetGoldBalanceState(this IAccountStateDelta states, Address address)
        {
            try
            {
                return new GoldBalanceState(address, states.GetBalance(address, Currencies.Gold));
            }
            catch (BalanceDoesNotExistsException)
            {
                return null;
            }
        }

        public static AvatarState GetAvatarState(this IAccountStateDelta states, Address address)
        {
            var serializedAvatar = states.GetState(address);
            if (serializedAvatar is null)
            {
                Log.Warning("No avatar state ({0})", address.ToHex());
                return null;
            }

            try
            {
                return new AvatarState((Bencodex.Types.Dictionary) serializedAvatar);
            }
            catch (InvalidCastException e)
            {
                Log.Error(
                    e,
                    "Invalid avatar state ({0}): {1}",
                    address.ToHex(),
                    serializedAvatar
                );

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
                Log.Error(
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
                Log.Warning("No weekly arena state ({0})", address.ToHex());
                return null;
            }

            try
            {
                return new WeeklyArenaState(iValue);
            }
            catch (InvalidCastException e)
            {
                Log.Error(
                    e,
                    "Invalid weekly arena state ({0}): {1}",
                    address.ToHex(),
                    iValue
                );

                return null;
            }
        }

        public static CombinationSlotState GetCombinationSlotState(this IAccountStateDelta states,
            Address avatarAddress, int index)
        {
            var address = avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    index
                )
            );
            var value = states.GetState(address);
            if (value is null)
            {
                Log.Warning("No combination slot state ({0})", address.ToHex());
                return null;
            }

            try
            {
                return new CombinationSlotState((Dictionary) value);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Unexpected error occurred during {nameof(GetCombinationSlotState)}()");
                throw;
            }
        }

        public static GameConfigState GetGameConfigState(this IAccountStateDelta states)
        {
            var value = states.GetState(GameConfigState.Address);
            if (value is null)
            {
                Log.Warning("No game config state ({0})", GameConfigState.Address.ToHex());
                return null;
            }

            try
            {
                return new GameConfigState((Dictionary) value);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Unexpected error occurred during {nameof(GetCombinationSlotState)}()");
                throw;
            }
        }

        public static RedeemCodeState GetRedeemCodeState(this IAccountStateDelta states)
        {
            var value = states.GetState(RedeemCodeState.Address);
            if (value is null)
            {
                Log.Warning("RedeemCodeState is null. ({0})", RedeemCodeState.Address.ToHex());
                return null;
            }

            try
            {
                return new RedeemCodeState((Dictionary) value);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Unexpected error occurred during {nameof(GetCombinationSlotState)}()");
                throw;
            }
        }
    }
}
