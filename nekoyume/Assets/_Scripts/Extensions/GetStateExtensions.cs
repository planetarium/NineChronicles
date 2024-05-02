#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Nekoyume.Blockchain;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;

namespace Nekoyume
{
    public static class GetStateExtensions
    {
        public static async Task<(List<ItemSlotState>, List<RuneSlotState>)> GetSlotStatesAsync(
            this IAgent agent, Address avatarAddress)
        {
            var itemAddresses = new List<Address>
            {
                ItemSlotState.DeriveAddress(avatarAddress, BattleType.Adventure),
                ItemSlotState.DeriveAddress(avatarAddress, BattleType.Arena),
                ItemSlotState.DeriveAddress(avatarAddress, BattleType.Raid)
            };
            var itemBulk = await agent.GetStateBulkAsync(
                ReservedAddresses.LegacyAccount,
                itemAddresses);
            var itemSlotStates = new List<ItemSlotState>();
            foreach (var value in itemBulk.Values)
            {
                if (value is List list)
                {
                    itemSlotStates.Add(new ItemSlotState(list));
                }
            }

            var runeAddresses = new List<Address>
            {
                RuneSlotState.DeriveAddress(avatarAddress, BattleType.Adventure),
                RuneSlotState.DeriveAddress(avatarAddress, BattleType.Arena),
                RuneSlotState.DeriveAddress(avatarAddress, BattleType.Raid)
            };
            var runeBulk = await Game.Game.instance.Agent.GetStateBulkAsync(
                ReservedAddresses.LegacyAccount,
                runeAddresses);
            var runeSlotStates = new List<RuneSlotState>();
            foreach (var value in runeBulk.Values)
            {
                if (value is List list)
                {
                    runeSlotStates.Add(new RuneSlotState(list));
                }
            }

            return (itemSlotStates, runeSlotStates);
        }

        public static async Task<AllRuneState> GetAllRuneStateAsync(
            this IAgent agent, Address avatarAddress)
        {
            AllRuneState allRuneState;

            var allRuneStateValue = await agent.GetStateAsync(
                Addresses.RuneState, avatarAddress);
            if (allRuneStateValue is List allRuneStateSerialized)
            {
                allRuneState = new AllRuneState(allRuneStateSerialized);
            }
            else
            {
                allRuneState = new AllRuneState();

                var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
                var runeAddresses = runeListSheet.Values.Select(row =>
                    RuneState.DeriveAddress(avatarAddress, row.Id));
                var stateBulk = await Game.Game.instance.Agent.GetStateBulkAsync(
                    ReservedAddresses.LegacyAccount, runeAddresses);
                foreach (var runeSerialized in stateBulk.Values.OfType<List>())
                {
                    allRuneState.AddRuneState(new RuneState(runeSerialized));
                }
            }

            return allRuneState;
        }

        public static AllRuneState GetAllRuneState(HashDigest<SHA256> hash, Address avatarAddress)
        {
            AllRuneState allRuneState;

            var allRuneStateValue = StateGetter.GetState(hash, Addresses.RuneState, avatarAddress);
            if (allRuneStateValue is List allRuneStateSerialized)
            {
                allRuneState = new AllRuneState(allRuneStateSerialized);
            }
            else
            {
                allRuneState = new AllRuneState();

                var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
                var runeAddresses = runeListSheet.Values.Select(row =>
                    RuneState.DeriveAddress(avatarAddress, row.Id));
                var stateBulk = StateGetter.GetStates(hash,
                    ReservedAddresses.LegacyAccount, runeAddresses);
                foreach (var runeSerialized in stateBulk.OfType<List>())
                {
                    allRuneState.AddRuneState(new RuneState(runeSerialized));
                }
            }

            return allRuneState;
        }

        public static async Task<CollectionState> GetCollectionStateAsync(
            this IAgent agent, Address avatarAddress)
        {
            var value = await agent.GetStateAsync(Addresses.Collection, avatarAddress);
            if (value is List list)
            {
                return new CollectionState(list);
            }

            return new CollectionState();
        }
    }
}
