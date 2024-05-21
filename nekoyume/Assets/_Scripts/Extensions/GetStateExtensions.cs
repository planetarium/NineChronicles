#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Blockchain;
using Nekoyume.Helper;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;
using Nekoyume.Module;

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

        public static async Task<SeasonInfo> GetAdventureBossLatestSeasonAsync(this IAgent agent)
        {
            var latestSeason = await agent.GetStateAsync(Addresses.AdventureBoss, AdventureBossModule.LatestSeasonAddress);
            if (latestSeason is null)
            {
                NcDebug.LogWarning("[AdventureBoss] No latest season");
                return new SeasonInfo(0, 0, 0, 0);
            }
            var result = new SeasonInfo((List)latestSeason);
            NcDebug.Log($"[AdventureBoss] Get LatestSeason SeasonId: {result.Season}  S:{result.StartBlockIndex}  E:{result.EndBlockIndex}  N:{result.NextStartBlockIndex}");
            return result;
        }

        public static async Task<SeasonInfo> GetAdventureBossLatestSeasonInfoAsync(this IAgent agent)
        {
            var latestSeason = await agent.GetAdventureBossLatestSeasonAsync();
            return latestSeason;
        }

        public static async Task<SeasonInfo> GetAdventureBossSeasonInfoAsync(this IAgent agent, long seasonId)
        {
            var seasonInfo = await agent.GetStateAsync(Addresses.AdventureBoss, new Address(AdventureBossHelper.GetSeasonAsAddressForm(seasonId)));
            if(seasonInfo is null)
            {
                NcDebug.LogWarning($"[AdventureBoss] No season info for {seasonId}");
                return null;
            }
            var result = new SeasonInfo((List)seasonInfo);
            NcDebug.Log($"[AdventureBoss] Get SeasonInfo SeasonId: {result.Season}  S:{result.StartBlockIndex}  E:{result.EndBlockIndex}  N:{result.NextStartBlockIndex}");
            return result;
        }

        public static async Task<BountyBoard> GetBountyBoardAsync(this IAgent agent, long seasonId)
        {
            var bountyBoard = await agent.GetStateAsync(Addresses.BountyBoard, new Address(AdventureBossHelper.GetSeasonAsAddressForm(seasonId)));
            if(bountyBoard is null)
            {
                NcDebug.LogWarning($"[AdventureBoss] No bounty board for {seasonId}");
                return null;
            }
            var result = new BountyBoard((List)bountyBoard);
            NcDebug.Log($"[AdventureBoss] Get BountyBoard Investors: {result.Investors.Count}");
            return result;
        }

        public static async Task<ExploreBoard> GetExploreBoardAsync(this IAgent agent, long seasonId)
        {
            var exploreBoard = await agent.GetStateAsync(Addresses.ExploreBoard, new Address(AdventureBossHelper.GetSeasonAsAddressForm(seasonId)));
            if (exploreBoard is null)
            {
                NcDebug.LogWarning($"[AdventureBoss] No explore board for {seasonId}");
                return null;
            }
            var result = new ExploreBoard((List)exploreBoard);
            NcDebug.Log($"[AdventureBoss] Get ExploreBoard ExplorerList: {result.ExplorerList.Count}");
            return result;
        }

        public static async Task<Explorer> GetExploreInfoAsync(this IAgent agent, Address avatarAddress, long seasonId)
        {
            var exploreInfo = await agent.GetStateAsync(Addresses.ExploreBoard, avatarAddress.Derive(AdventureBossHelper.GetSeasonAsAddressForm(seasonId)));
            if (exploreInfo is null)
            {
                NcDebug.LogWarning($"[AdventureBoss] No explore info for {avatarAddress}");
                return null;
            }
            var result = new Explorer(exploreInfo);
            NcDebug.Log($"[AdventureBoss] Get ExploreInfo Avatar: {avatarAddress}  Score: {result.Score}  Floor:{result.Floor}");
            return result;
        }
    }
}
