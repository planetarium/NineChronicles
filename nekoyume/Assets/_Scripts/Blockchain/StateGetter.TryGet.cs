using System;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Common;
using Libplanet.Crypto;
using Nekoyume.Exceptions;
using Nekoyume.Model.Arena;
using Nekoyume.Model.Stake;
using Nekoyume.Model.State;

namespace Nekoyume.Blockchain
{
    public static partial class StateGetter
    {
        public static bool TryGetState(
            HashDigest<SHA256> hash,
            Address accountAddress,
            Address address,
            out IValue value)
        {
            value = Game.Game.instance.Agent.GetStateAsync(hash, accountAddress, address).Result;
            return value is not Null;
        }

        public static bool TryGetAvatarState(
            HashDigest<SHA256> hash,
            Address avatarAddress,
            out AvatarState avatarState)
        {
            try
            {
                avatarState = GetAvatarState(hash, avatarAddress);
                return true;
            }
            catch (StateNullException e)
            {
                avatarState = null;
                return false;
            }
        }

        public static bool TryGetAvatarState(
            HashDigest<SHA256> hash,
            Address agentAddress,
            Address avatarAddress,
            out AvatarState avatarState)
        {
            avatarState = null;
            if (!TryGetAvatarState(hash, avatarAddress, out var value))
            {
                return false;
            }
            avatarState = value;

            return avatarState.agentAddress == agentAddress;
        }

        public static bool TryGetStakeStateV2(
            HashDigest<SHA256> hash,
            Address address,
            out StakeStateV2 stakeStateV2)
        {
            try
            {
                if (GetStakeStateV2(hash, address) is { } state)
                {
                    stakeStateV2 = state;
                    return true;
                }
            }
            catch (Exception e)
            {
                // ignored
            }

            stakeStateV2 = default;
            return false;
        }

        public static bool TryGetCombinationSlotState(
            HashDigest<SHA256> hash,
            Address avatarAddress,
            int index,
            out CombinationSlotState combinationSlotState)
        {
            try
            {
                combinationSlotState = GetCombinationSlotState(hash, avatarAddress, index);
                return true;
            }
            catch (Exception)
            {
                combinationSlotState = null;
                return false;
            }
        }

        public static bool TryGetArenaScore(
            HashDigest<SHA256> hash,
            Address arenaScoreAddress,
            out ArenaScore arenaScore)
        {
            try
            {
                arenaScore = GetArenaScore(hash, arenaScoreAddress);
                return true;
            }
            catch (Exception)
            {
                arenaScore = null;
                return false;
            }
        }

        public static bool TryGetRedeemCodeState(
            HashDigest<SHA256> hash,
            out RedeemCodeState redeemCodeState)
        {
            try
            {
                redeemCodeState = GetRedeemCodeState(hash);
                return true;
            }
            catch (Exception)
            {
                redeemCodeState = null;
                return false;
            }
        }
    }
}
