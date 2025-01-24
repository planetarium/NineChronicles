using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Exceptions;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Stake;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Blockchain
{
    public static partial class StateGetter
    {
        public static IValue GetState(HashDigest<SHA256> hash, Address accountAddress, Address address)
        {
            return Game.Game.instance.Agent.GetStateAsync(hash, accountAddress, address).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public static IReadOnlyList<IValue> GetStates(
            HashDigest<SHA256> hash,
            Address accountAddress,
            IEnumerable<Address> addresses)
        {
            return Game.Game.instance.Agent.GetStateBulkAsync(hash, accountAddress, addresses).Result.Values.ToArray();
        }

        public static FungibleAssetValue GetBalance(
            HashDigest<SHA256> hash,
            Address address,
            Currency currency)
        {
            return Game.Game.instance.Agent.GetBalanceAsync(hash, address, currency).Result;
        }

        public static GameConfigState GetGameConfigState(HashDigest<SHA256> hash)
        {
            var value = GetState(hash, ReservedAddresses.LegacyAccount, GameConfigState.Address);
            if (value is null or Null)
            {
                Log.Warning("No game config state ({GameConfigStateAddress})", GameConfigState.Address.ToHex());
                return null;
            }

            try
            {
                return new GameConfigState((Dictionary)value);
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected error occurred during {GameConfigStateName}()", nameof(GetGameConfigState));
                throw;
            }
        }

        public static AvatarState GetAvatarState(HashDigest<SHA256> hash, Address avatarAddress)
        {
            var result = Game.Game.instance.Agent
                .GetAvatarStatesAsync(hash, new[] { avatarAddress, }).Result;

            if (result.TryGetValue(avatarAddress, out var value))
            {
                return value;
            }

            throw new StateNullException(Addresses.Avatar, avatarAddress);
        }

        public static AgentState GetAgentState(HashDigest<SHA256> hash, Address address)
        {
            return Game.Game.instance.Agent.GetAgentStateAsync(hash, address).Result;
        }

        public static GoldBalanceState GetGoldBalanceState(
            HashDigest<SHA256> hash,
            Address address,
            Currency currency)
        {
            return new GoldBalanceState(
                address,
                Game.Game.instance.Agent.GetBalanceAsync(hash, address, currency).Result);
        }

        public static StakeState? GetStakeState(HashDigest<SHA256> hash, Address address)
        {
            var stakeStateAddr = StakeState.DeriveAddress(address);
            var serialized = Game.Game.instance.Agent.GetState(
                hash,
                ReservedAddresses.LegacyAccount,
                stakeStateAddr);
            if (serialized is null or Null)
            {
                Log.Warning("No stake state ({Address})", address.ToHex());
                throw new StateNullException(ReservedAddresses.LegacyAccount, address);
            }

            try
            {
                var stakeState = StakeStateUtils.MigrateV1ToV2(serialized, GetGameConfigState(hash));
                return new StakeState(
                    stakeState.Value.Contract,
                    stakeState.Value.StartedBlockIndex,
                    stakeState.Value.ReceivedBlockIndex,
                    stateVersion: 3);
            }
            catch (InvalidCastException e)
            {
                Log.Error(
                    e,
                    "Invalid stake state ({Address}): {Serialized}",
                    address.ToHex(),
                    serialized
                );

                return null;
            }
        }

        // Caution: This method assumes that the inventory is migrated to V2.
        public static Inventory GetInventory(
            HashDigest<SHA256> hash,
            Address inventoryAddr)
        {
            var inventoryState = GetState(hash, Addresses.Inventory, inventoryAddr);
            if (inventoryState is null or Null)
            {
                throw new StateNullException(Addresses.Inventory, inventoryAddr);
            }

            return new Inventory((List)inventoryState);
        }

        [Obsolete("Use AllCombinationSlotState instead.")]
        public static CombinationSlotState GetCombinationSlotState(
            HashDigest<SHA256> hash,
            Address avatarAddress,
            int index)
        {
            var address = avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    index
                )
            );
            var value = Game.Game.instance.Agent.GetState(
                hash,
                ReservedAddresses.LegacyAccount,
                address);
            if (value is null or Null)
            {
                throw new StateNullException(ReservedAddresses.LegacyAccount, address);
            }

            try
            {
                return new CombinationSlotState((Dictionary)value);
            }
            catch (Exception e)
            {
                Log.Error(
                    e,
                    "Unexpected error occurred during {CombinationSlotStateName}()",
                    nameof(GetCombinationSlotState));
                throw;
            }
        }

        public static RedeemCodeState GetRedeemCodeState(HashDigest<SHA256> hash)
        {
            var value = Game.Game.instance.Agent.GetState(
                hash,
                ReservedAddresses.LegacyAccount,
                RedeemCodeState.Address);
            if (value is null or Null)
            {
                Log.Warning(
                    "RedeemCodeState is null or Null. ({RedeemCodeStateAddress})",
                    RedeemCodeState.Address.ToHex());
                throw new StateNullException(ReservedAddresses.LegacyAccount, RedeemCodeState.Address);
            }

            try
            {
                return new RedeemCodeState((Dictionary)value);
            }
            catch (Exception e)
            {
                Log.Error(
                    e,
                    "Unexpected error occurred during {CombinationSlotStateName}()",
                    nameof(GetRedeemCodeState));
                throw;
            }
        }

        public static ArenaScore GetArenaScore(
            HashDigest<SHA256> hash,
            Address arenaScoreAddress)
        {
            var value = Game.Game.instance.Agent.GetState(
                hash,
                ReservedAddresses.LegacyAccount,
                arenaScoreAddress);
            if (value is List list)
            {
                return new ArenaScore(list);
            }

            throw new StateNullException(ReservedAddresses.LegacyAccount, arenaScoreAddress);
        }

        public static ItemSlotState GetItemSlotState(
            HashDigest<SHA256> hash,
            Address avatarAddress,
            BattleType battleType)
        {
            var itemSlotAddress = ItemSlotState.DeriveAddress(avatarAddress, battleType);
            var value = GetState(hash, ReservedAddresses.LegacyAccount, itemSlotAddress);
            if (value is List list)
            {
                return new ItemSlotState(list);
            }

            throw new StateNullException(ReservedAddresses.LegacyAccount, itemSlotAddress);
        }

        public static RuneSlotState GetRuneSlotState(
            HashDigest<SHA256> hash,
            Address avatarAddress,
            BattleType battleType)
        {
            var runeSlotAddress = RuneSlotState.DeriveAddress(avatarAddress, battleType);
            var value = GetState(hash, ReservedAddresses.LegacyAccount, runeSlotAddress);
            if (value is List list)
            {
                return new RuneSlotState(list);
            }

            throw new StateNullException(ReservedAddresses.LegacyAccount, runeSlotAddress);
        }

        public static CollectionState GetCollectionState(
            HashDigest<SHA256> hash,
            Address avatarAddress)
        {
            var value = GetState(hash, Addresses.Collection, avatarAddress);
            if (value is List list)
            {
                return new CollectionState(list);
            }

            return new CollectionState();
        }

        public static MailBox GetMailBox(
            HashDigest<SHA256> hash,
            Address avatarAddress)
        {
            var avatarValue = GetState(hash, Addresses.Avatar, avatarAddress);
            if (avatarValue is not List avatarList)
            {
                NcDebug.LogError($"Failed to get avatar state: {avatarAddress}, {avatarValue}");
                return null;
            }

            if (avatarList.Count < 9 || avatarList[8] is not List mailBoxList)
            {
                NcDebug.LogError($"Failed to get mail box: {avatarAddress}");
                return null;
            }

            return new MailBox(mailBoxList);
        }

        public static async Task<long> GetPatrolRewardReceivedBlockIndex(HashDigest<SHA256>
            hash, Address avatarAddress)
        {
            var state = await Game.Game.instance.Agent.GetStateAsync(hash, Addresses.PatrolReward, avatarAddress);
            return state is Integer index
                ? index
                : 0L;
        }
    }
}
