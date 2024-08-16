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
        
        public static async Task<AllCombinationSlotState> GetAllCombinationSlotStateAsync(
            this IAgent agent, Address avatarAddress)
        {
            AllCombinationSlotState allCombinationSlotState;
            
            var value = await agent.GetStateAsync(Addresses.CombinationSlot, avatarAddress);
            if (value is List list)
            {
                allCombinationSlotState = new AllCombinationSlotState(list);
            }
            else
            {
                allCombinationSlotState = AllCombinationSlotState.MigrationLegacySlotState(slotIndex =>
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    return StateGetter.GetCombinationSlotState(avatarAddress, slotIndex);
#pragma warning restore CS0618 // Type or member is obsolete
                }, avatarAddress);
            }

            return allCombinationSlotState;
        }

        public static AllCombinationSlotState GetAllCombinationSlotState(HashDigest<SHA256> hash, Address avatarAddress)
        {
            AllCombinationSlotState allCombinationSlotState;

            var allCombinationSlotStateValue = StateGetter.GetState(hash, Addresses.CombinationSlot, avatarAddress);
            if (allCombinationSlotStateValue is List allCombinationSlotStateSerialized)
            {
                allCombinationSlotState = new AllCombinationSlotState(allCombinationSlotStateSerialized);
            }
            else
            {
                allCombinationSlotState = AllCombinationSlotState.MigrationLegacySlotState(slotIndex =>
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    return StateGetter.GetCombinationSlotState(hash, avatarAddress, slotIndex);
#pragma warning restore CS0618 // Type or member is obsolete
                }, avatarAddress);
            }

            return allCombinationSlotState;
        }

        public static CombinationSlotState GetCombinationSlotState(HashDigest<SHA256> hash, Address avatarAddress, int slotIndex)
        {
            var allSlot = GetAllCombinationSlotState(hash, avatarAddress);
            return allSlot.GetSlot(slotIndex);
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

        private static long adventureBossLatestSeasonLastBlockIndex = 0;
        private static HashDigest<SHA256> adventureBossLatestSeasonLastStateRootHash;

        public static async Task<SeasonInfo> GetAdventureBossLatestSeasonAsync(this IAgent agent, HashDigest<SHA256>? stateRootHash = null, long blockIndex = 0)
        {
            var stateroot = stateRootHash == null ? "null" : stateRootHash.Value.ToString();
            NcDebug.Log($"[AdventureBoss] Get LatestSeason blockIndex: {blockIndex} stateRootHash: {stateroot} agentBlockIndex: {agent.BlockIndex}");
            IValue latestSeason;
            if (stateRootHash == null)
            {
                if (agent.BlockIndex <= adventureBossLatestSeasonLastBlockIndex)
                {
                    latestSeason = await agent.GetStateAsync(adventureBossLatestSeasonLastStateRootHash, Addresses.AdventureBoss, AdventureBossModule.LatestSeasonAddress);
                }
                else
                {
                    adventureBossLatestSeasonLastBlockIndex = agent.BlockIndex - 1;
                    latestSeason = await agent.GetStateAsync(Addresses.AdventureBoss, AdventureBossModule.LatestSeasonAddress);
                }
            }
            else
            {
                adventureBossLatestSeasonLastBlockIndex = blockIndex;
                adventureBossLatestSeasonLastStateRootHash = stateRootHash.Value;
                latestSeason = await agent.GetStateAsync(adventureBossLatestSeasonLastStateRootHash, Addresses.AdventureBoss, AdventureBossModule.LatestSeasonAddress);
            }

            if (latestSeason is List list)
            {
                var result = new SeasonInfo(list);
                NcDebug.Log($"[AdventureBoss] Get LatestSeason SeasonId: {result.Season}  S:{result.StartBlockIndex}  E:{result.EndBlockIndex}  N:{result.NextStartBlockIndex}");
                return result;
            }

            NcDebug.LogWarning("[AdventureBoss] No latest season");
            return new SeasonInfo(0, 0, 0, 0);
        }

        private static long adventureBossSeasonInfoLastBlockIndex = 0;
        private static HashDigest<SHA256> adventureBossSeasonInfoLastStateRootHash;

        public static async Task<SeasonInfo> GetAdventureBossSeasonInfoAsync(this IAgent agent, long seasonId, HashDigest<SHA256>? stateRootHash = null, long blockIndex = 0)
        {
            var stateroot = stateRootHash == null ? "null" : stateRootHash.Value.ToString();
            NcDebug.Log($"[AdventureBoss] Get SeasonInfo Season: {seasonId} blockIndex: {blockIndex} stateRootHash: {stateroot} agentBlockIndex: {agent.BlockIndex}");
            IValue seasonInfo;
            if (stateRootHash == null)
            {
                if (agent.BlockIndex <= adventureBossSeasonInfoLastBlockIndex)
                {
                    seasonInfo = await agent.GetStateAsync(adventureBossSeasonInfoLastStateRootHash, Addresses.AdventureBoss, new Address(AdventureBossHelper.GetSeasonAsAddressForm(seasonId)));
                }
                else
                {
                    adventureBossSeasonInfoLastBlockIndex = agent.BlockIndex - 1;
                    seasonInfo = await agent.GetStateAsync(Addresses.AdventureBoss, new Address(AdventureBossHelper.GetSeasonAsAddressForm(seasonId)));
                }
            }
            else
            {
                adventureBossSeasonInfoLastBlockIndex = blockIndex;
                adventureBossSeasonInfoLastStateRootHash = stateRootHash.Value;
                seasonInfo = await agent.GetStateAsync(adventureBossSeasonInfoLastStateRootHash, Addresses.AdventureBoss, new Address(AdventureBossHelper.GetSeasonAsAddressForm(seasonId)));
            }

            if (seasonInfo is List list)
            {
                var result = new SeasonInfo(list);
                NcDebug.Log($"[AdventureBoss] Get SeasonInfo SeasonId: {result.Season}  S:{result.StartBlockIndex}  E:{result.EndBlockIndex}  N:{result.NextStartBlockIndex}");
                return result;
            }

            NcDebug.LogWarning($"[AdventureBoss] No season info for {seasonId}");
            return null;
        }

        private static long bountyBoardLastBlockIndex = 0;
        private static HashDigest<SHA256> bountyBoardLastStateRootHash;

        public static async Task<BountyBoard> GetBountyBoardAsync(this IAgent agent, long seasonId, HashDigest<SHA256>? stateRootHash = null, long blockIndex = 0)
        {
            var stateroot = stateRootHash == null ? "null" : stateRootHash.Value.ToString();
            NcDebug.Log($"[AdventureBoss] Get BountyBoard Season: {seasonId} blockIndex: {blockIndex} stateRootHash: {stateroot} agentBlockIndex: {agent.BlockIndex}");
            IValue bountyBoard;
            if (stateRootHash == null)
            {
                if (agent.BlockIndex <= bountyBoardLastBlockIndex)
                {
                    bountyBoard = await agent.GetStateAsync(bountyBoardLastStateRootHash, Addresses.BountyBoard, new Address(AdventureBossHelper.GetSeasonAsAddressForm(seasonId)));
                }
                else
                {
                    bountyBoardLastBlockIndex = agent.BlockIndex - 1;
                    bountyBoard = await agent.GetStateAsync(Addresses.BountyBoard, new Address(AdventureBossHelper.GetSeasonAsAddressForm(seasonId)));
                }
            }
            else
            {
                bountyBoardLastBlockIndex = blockIndex;
                bountyBoardLastStateRootHash = stateRootHash.Value;
                bountyBoard = await agent.GetStateAsync(bountyBoardLastStateRootHash, Addresses.BountyBoard, new Address(AdventureBossHelper.GetSeasonAsAddressForm(seasonId)));
            }

            if (bountyBoard is List list)
            {
                var result = new BountyBoard(list);
                NcDebug.Log($"[AdventureBoss] Get BountyBoard Investors: {result.Investors.Count}");
                return result;
            }

            NcDebug.LogWarning($"[AdventureBoss] No bounty board for {seasonId}");
            return null;
        }

        private static long exploreBoardLastBlockIndex = 0;
        private static HashDigest<SHA256> exploreBoardLastStateRootHash;

        public static async Task<ExploreBoard> GetExploreBoardAsync(this IAgent agent, long seasonId, HashDigest<SHA256>? stateRootHash = null, long blockIndex = 0)
        {
            var stateroot = stateRootHash == null ? "null" : stateRootHash.Value.ToString();
            NcDebug.Log($"[AdventureBoss] Get ExploreBoard Season: {seasonId} blockIndex: {blockIndex} stateRootHash: {stateroot} agentBlockIndex: {agent.BlockIndex}");
            IValue exploreBoard;
            if (stateRootHash == null)
            {
                if (agent.BlockIndex <= exploreBoardLastBlockIndex)
                {
                    exploreBoard = await agent.GetStateAsync(exploreBoardLastStateRootHash, Addresses.ExploreBoard, new Address(AdventureBossHelper.GetSeasonAsAddressForm(seasonId)));
                }
                else
                {
                    exploreBoardLastBlockIndex = agent.BlockIndex - 1;
                    exploreBoard = await agent.GetStateAsync(Addresses.ExploreBoard, new Address(AdventureBossHelper.GetSeasonAsAddressForm(seasonId)));
                }
            }
            else
            {
                exploreBoardLastBlockIndex = blockIndex;
                exploreBoardLastStateRootHash = stateRootHash.Value;
                exploreBoard = await agent.GetStateAsync(exploreBoardLastStateRootHash, Addresses.ExploreBoard, new Address(AdventureBossHelper.GetSeasonAsAddressForm(seasonId)));
            }

            if (exploreBoard is List list)
            {
                var result = new ExploreBoard(list);
                NcDebug.Log($"[AdventureBoss] Get ExploreBoard ExplorerList: {result.ExplorerCount}");
                return result;
            }

            NcDebug.LogWarning($"[AdventureBoss] No explore board for {seasonId}");
            return null;
        }

        private static long exploreInfoLastBlockIndex = 0;
        private static HashDigest<SHA256> exploreInfoLastStateRootHash;

        public static async Task<Explorer> GetExploreInfoAsync(this IAgent agent, Address avatarAddress, long seasonId, HashDigest<SHA256>? stateRootHash = null, long blockIndex = 0)
        {
            var stateroot = stateRootHash == null ? "null" : stateRootHash.Value.ToString();
            NcDebug.Log($"[AdventureBoss] Get ExploreInfo Avatar: {avatarAddress}  Season: {seasonId} blockIndex: {blockIndex} stateRootHash: {stateroot} agentBlockIndex: {agent.BlockIndex}");
            IValue exploreInfo;
            if (stateRootHash == null)
            {
                if (agent.BlockIndex <= exploreInfoLastBlockIndex)
                {
                    exploreInfo = await agent.GetStateAsync(exploreInfoLastStateRootHash, Addresses.ExploreBoard, avatarAddress.Derive(AdventureBossHelper.GetSeasonAsAddressForm(seasonId)));
                }
                else
                {
                    exploreInfoLastBlockIndex = agent.BlockIndex - 1;
                    exploreInfo = await agent.GetStateAsync(Addresses.ExploreBoard, avatarAddress.Derive(AdventureBossHelper.GetSeasonAsAddressForm(seasonId)));
                }
            }
            else
            {
                exploreInfoLastBlockIndex = blockIndex;
                exploreInfoLastStateRootHash = stateRootHash.Value;
                exploreInfo = await agent.GetStateAsync(exploreInfoLastStateRootHash, Addresses.ExploreBoard, avatarAddress.Derive(AdventureBossHelper.GetSeasonAsAddressForm(seasonId)));
            }

            if (exploreInfo == null || exploreInfo is Null)
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
