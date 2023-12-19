using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Exceptions;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stake;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Blockchain
{
    public static partial class StateGetter
    {
        public static IValue GetState(Address address, HashDigest<SHA256> hash) =>
            Game.Game.instance.Agent.GetStateAsync(address, hash).ConfigureAwait(false).GetAwaiter().GetResult();

        public static IReadOnlyList<IValue> GetStates(
            IReadOnlyList<Address> addresses,
            HashDigest<SHA256> hash) =>
            Game.Game.instance.Agent.GetStateBulkAsync(addresses, hash).Result.Values.ToArray();

        public static FungibleAssetValue GetBalance(
            Address address,
            Currency currency,
            HashDigest<SHA256> hash) =>
            Game.Game.instance.Agent.GetBalanceAsync(address, currency, hash).Result;

        public static GameConfigState GetGameConfigState(HashDigest<SHA256> hash)
        {
            var value = GetState(GameConfigState.Address, hash);
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

        public static AvatarState GetAvatarState(Address avatarAddress, HashDigest<SHA256> hash)
        {
            var result = Game.Game.instance.Agent.GetAvatarStatesAsync(
                    new[]
                    {
                        avatarAddress,
                    },
                    hash)
                .Result;

            if (result.TryGetValue(avatarAddress, out AvatarState value))
            {
                return value;
            }

            throw new StateNullException(avatarAddress);
        }

        public static AgentState GetAgentState(Address address, HashDigest<SHA256> hash)
        {
            var serializedAgent = Game.Game.instance.Agent.GetState(address, hash);
            if (serializedAgent is null or Null)
            {
                Log.Warning("No agent state ({Address})", address.ToHex());
                return null;
            }

            try
            {
                return new AgentState((Dictionary)serializedAgent);
            }
            catch (InvalidCastException e)
            {
                Log.Error(
                    e,
                    "Invalid agent state ({Address}): {SerializedAgent}",
                    address.ToHex(),
                    serializedAgent
                );

                return null;
            }
        }

        public static GoldBalanceState GetGoldBalanceState(
            Address address,
            Currency currency,
            HashDigest<SHA256> hash) =>
            new GoldBalanceState(
                address,
                Game.Game.instance.Agent.GetBalanceAsync(address, currency, hash).Result);

        public static StakeStateV2? GetStakeStateV2(Address address, HashDigest<SHA256> hash)
        {
            var stakeStateAddr = StakeStateV2.DeriveAddress(address);
            IValue serialized = Game.Game.instance.Agent.GetState(stakeStateAddr, hash);
            if (serialized is null or Null)
            {
                Log.Warning("No stake state ({Address})", address.ToHex());
                throw new StateNullException(address);
            }

            try
            {
                return StakeStateUtils.Migrate(serialized, GetGameConfigState(hash));
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

        public static Inventory GetInventory(
            Address inventoryAddr,
            HashDigest<SHA256> hash)
        {
            var inventoryState = GetState(inventoryAddr, hash);
            if (inventoryState is null or Null)
            {
                throw new StateNullException(inventoryAddr);
            }

            return new Inventory((List)inventoryState);
        }

        public static CombinationSlotState GetCombinationSlotState(
            Address avatarAddress,
            int index,
            HashDigest<SHA256> hash)
        {
            var address = avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    index
                )
            );
            var value = Game.Game.instance.Agent.GetState(address, hash);
            if (value is null or Null)
            {
                throw new StateNullException(address);
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
            var value = Game.Game.instance.Agent.GetState(RedeemCodeState.Address, hash);
            if (value is null or Null)
            {
                Log.Warning(
                    "RedeemCodeState is null or Null. ({RedeemCodeStateAddress})",
                    RedeemCodeState.Address.ToHex());
                throw new StateNullException(RedeemCodeState.Address);
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
                    nameof(GetCombinationSlotState));
                throw;
            }
        }

        public static ArenaScore GetArenaScore(
            Address arenaScoreAddress,
            HashDigest<SHA256> hash)
        {
            var value = Game.Game.instance.Agent.GetState(arenaScoreAddress, hash);
            if (value is List list)
            {
                return new ArenaScore(list);
            }

            throw new StateNullException(arenaScoreAddress);
        }

        public static ItemSlotState GetItemSlotState(
            Address avatarAddress,
            BattleType battleType,
            HashDigest<SHA256> hash)
        {
            var itemSlotAddress = ItemSlotState.DeriveAddress(avatarAddress, battleType);
            var value = GetState(itemSlotAddress,hash);
            if (value is List list)
            {
                return new ItemSlotState(list);
            }

            throw new StateNullException(itemSlotAddress);
        }

        public static RuneSlotState GetRuneSlotState(
            Address avatarAddress,
            BattleType battleType,
            HashDigest<SHA256> hash)
        {
            var runeSlotAddress = RuneSlotState.DeriveAddress(avatarAddress, battleType);
            var value = GetState(runeSlotAddress, hash);
            if (value is List list)
            {
                return new RuneSlotState(list);
            }

            throw new StateNullException(runeSlotAddress);
        }
    }
}
