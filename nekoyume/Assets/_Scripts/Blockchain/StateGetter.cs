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
using Nekoyume.Model.Item;
using Nekoyume.Model.Stake;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Blockchain
{
    public static partial class StateGetter
    {
        public static IValue GetState(Address address, HashDigest<SHA256> hash) =>
            Game.Game.instance.Agent.GetState(address, hash);

        public static bool TryGetState(Address address, HashDigest<SHA256> hash, out IValue value)
        {
            value = Game.Game.instance.Agent.GetState(address, hash);
            return value is not Null;
        }

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
            var value = Game.Game.instance.Agent.GetState(GameConfigState.Address, hash);
            if (value is Null)
            {
                Log.Warning("No game config state ({0})", GameConfigState.Address.ToHex());
                return null;
            }

            try
            {
                return new GameConfigState((Dictionary)value);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Unexpected error occurred during {nameof(GetGameConfigState)}()");
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
            if (serializedAgent is Null)
            {
                Log.Warning("No agent state ({0})", address.ToHex());
                return null;
            }

            try
            {
                return new AgentState((Bencodex.Types.Dictionary)serializedAgent);
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

        public static GoldBalanceState GetGoldBalanceState(
            Address address,
            Currency currency,
            HashDigest<SHA256> hash) =>
            new GoldBalanceState(
                address,
                Game.Game.instance.Agent.GetBalanceAsync(address, currency, hash).Result);

        public static StakeStateV2? GetStakeStateV2(Address address, HashDigest<SHA256> hash)
        {
            IValue serialized = Game.Game.instance.Agent.GetState(address, hash);
            if (serialized is Null)
            {
                Log.Warning("No stake state ({0})", address.ToHex());
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
                    "Invalid stake state ({0}): {1}",
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
            var inventoryState = Game.Game.instance.Agent.GetState(inventoryAddr, hash);
            if (inventoryState is null || inventoryState is Null)
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
            if (value is Null)
            {
                throw new StateNullException(address);
            }

            try
            {
                return new CombinationSlotState((Dictionary)value);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Unexpected error occurred during {nameof(GetCombinationSlotState)}()");
                throw;
            }
        }


        public static RedeemCodeState GetRedeemCodeState(HashDigest<SHA256> hash)
        {
            var value = Game.Game.instance.Agent.GetState(RedeemCodeState.Address, hash);
            if (value is Null)
            {
                Log.Warning("RedeemCodeState is null. ({0})", RedeemCodeState.Address.ToHex());
                throw new StateNullException(RedeemCodeState.Address);
            }

            try
            {
                return new RedeemCodeState((Dictionary)value);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Unexpected error occurred during {nameof(GetCombinationSlotState)}()");
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
    }
}
