using System;
using System.Security.Cryptography;
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
        public static bool TryGetAvatarState(
            Address avatarAddress,
            HashDigest<SHA256> hash,
            out AvatarState avatarState)
        {
            try
            {
                avatarState = GetAvatarState(avatarAddress, hash);
                return true;
            }
            catch (StateNullException e)
            {
                avatarState = null;
                return false;
            }
        }

        public static bool TryGetAvatarState(
            Address agentAddress,
            Address avatarAddress,
            HashDigest<SHA256> hash,
            out AvatarState avatarState)
        {
            avatarState = null;
            if (!TryGetAvatarState(avatarAddress, hash, out var value))
            {
                return false;
            }
            avatarState = value;

            return avatarState.agentAddress == agentAddress;
        }

        public static bool TryGetStakeStateV2(Address address, HashDigest<SHA256> hash, out StakeStateV2 stakeStateV2)
        {
            if(GetStakeStateV2(address, hash) is { } state)
            {
                stakeStateV2 = state;
                return true;
            }

            stakeStateV2 = default;
            return false;
        }

        public static bool TryGetCombinationSlotState(
            Address avatarAddress,
            int index,
            HashDigest<SHA256> hash,
            out CombinationSlotState combinationSlotState)
        {
            try
            {
                combinationSlotState = GetCombinationSlotState(avatarAddress, index, hash);
                return true;
            }
            catch (Exception)
            {
                combinationSlotState = null;
                return false;
            }
        }

        public static bool TryGetArenaScore(
            Address arenaScoreAddress,
            HashDigest<SHA256> hash,
            out ArenaScore arenaScore)
        {
            try
            {
                arenaScore = GetArenaScore(arenaScoreAddress, hash);
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
