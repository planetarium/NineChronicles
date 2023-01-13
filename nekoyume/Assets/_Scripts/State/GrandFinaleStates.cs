using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.GrandFinale;
using Nekoyume.Model.State;
using Nekoyume.TableData.GrandFinale;
using UnityEngine;
using static Lib9c.SerializeKeys;

namespace Nekoyume.State
{
    public class GrandFinaleStates
    {
        public GrandFinaleParticipant[] GrandFinaleParticipants { get; private set; } =
            Array.Empty<GrandFinaleParticipant>();

        public PlayerGrandFinaleParticipant GrandFinalePlayer { get; private set; }

        public bool IsUpdating { get; private set; } = false;

        private long _participantsUpdatedBlockIndex;

        public class GrandFinaleParticipant
        {
            public readonly Address AvatarAddr;
            public readonly int Score;
            public readonly int Rank;
            public readonly AvatarState AvatarState;
            public readonly ItemSlotState ItemSlotState;
            public readonly RuneSlotState RuneSlotState;
            public readonly List<RuneState> RuneStates;

            public GrandFinaleParticipant(
                Address avatarAddr,
                int score,
                int rank,
                AvatarState avatarState,
                ItemSlotState itemSlotState,
                RuneSlotState runeSlotState,
                List<RuneState> runeStates)
            {
                AvatarAddr = avatarAddr;
                Score = score;
                Rank = rank;
                AvatarState = avatarState;
                ItemSlotState = itemSlotState;
                RuneSlotState = runeSlotState;
                RuneStates = runeStates;
            }

            public GrandFinaleParticipant(GrandFinaleParticipant value)
            {
                AvatarAddr = value.AvatarAddr;
                Score = value.Score;
                Rank = value.Rank;
                AvatarState = value.AvatarState;
                ItemSlotState = value.ItemSlotState;
                RuneSlotState = value.RuneSlotState;
                RuneStates = value.RuneStates;
            }
        }

        public class PlayerGrandFinaleParticipant : GrandFinaleParticipant
        {
            public GrandFinaleInformation CurrentInfo;

            public PlayerGrandFinaleParticipant(
                Address avatarAddr,
                int score,
                int rank,
                AvatarState avatarState,
                ItemSlotState itemSlotState,
                RuneSlotState runeSlotState,
                List<RuneState> runeStates,
                GrandFinaleInformation currentInfo)
                : base(
                    avatarAddr,
                    score,
                    rank,
                    avatarState,
                    itemSlotState,
                    runeSlotState,
                    runeStates)
            {
                CurrentInfo = currentInfo;
            }

            public PlayerGrandFinaleParticipant(
                GrandFinaleParticipant grandFinaleParticipant,
                GrandFinaleInformation currentInfo)
                : base(grandFinaleParticipant)
            {
                CurrentInfo = currentInfo;
            }
        }

        public async UniTask<GrandFinaleParticipant[]>
            UpdateGrandFinaleParticipantsOrderedWithScoreAsync()
        {
            var states = States.Instance;
            var agent = Game.Game.instance.Agent;
            var tableSheets = TableSheets.Instance;
            var avatarAddress = states.CurrentAvatarState?.address;
            if (!avatarAddress.HasValue)
            {
                GrandFinaleParticipants = Array.Empty<GrandFinaleParticipant>();
                return GrandFinaleParticipants;
            }

            if (_participantsUpdatedBlockIndex == agent.BlockIndex)
            {
                return GrandFinaleParticipants;
            }

            _participantsUpdatedBlockIndex = agent.BlockIndex;

            var currentAvatar = states.CurrentAvatarState;
            var currentAvatarAddr = currentAvatar.address;
            var currentGrandFinaleData = tableSheets.GrandFinaleScheduleSheet.GetRowByBlockIndex(agent.BlockIndex);
            if (currentGrandFinaleData is null)
            {
                return Array.Empty<GrandFinaleParticipant>();
            }

            if (!tableSheets.GrandFinaleParticipantsSheet.TryGetValue(currentGrandFinaleData.Id, out var row)
                || !row.Participants.Any())
            {
                Debug.Log(
                    $"Failed to get {nameof(GrandFinaleParticipantsSheet)} with {currentGrandFinaleData.Id}");
                GrandFinalePlayer = null;
                return Array.Empty<GrandFinaleParticipant>();
            }

            IsUpdating = true;
            var avatarAddrList = row.Participants;
            var isGrandFinaleParticipant = avatarAddrList.Contains(currentAvatarAddr);
            var scoreDeriveString = string.Format(
                CultureInfo.InvariantCulture,
                BattleGrandFinale.ScoreDeriveKey,
                row.GrandFinaleId);
            var avatarAndScoreAddrList = avatarAddrList
                .Select(avatarAddr => (avatarAddr,
                    avatarAddr.Derive(scoreDeriveString))
                ).ToArray();
            // NOTE: If addresses is too large, and split and get separately.
            var scores = await agent.GetStateBulk(
                avatarAndScoreAddrList.Select(tuple => tuple.Item2));
            var avatarAddrAndScores = avatarAndScoreAddrList
                .Select(tuple =>
                {
                    var (avatarAddr, scoreAddr) = tuple;
                    return (
                        avatarAddr,
                        scores[scoreAddr] is Integer score
                            ? (int)score
                            : BattleGrandFinale.DefaultScore
                    );
                })
                .ToArray();
            var avatarAddrAndScoresWithRank =
                AddRank(avatarAddrAndScores, currentAvatarAddr);
            PlayerGrandFinaleParticipant playerGrandFinaleParticipant = null;
            var playerScore = 0;
            if (isGrandFinaleParticipant)
            {
                try
                {
                    var playerTuple = avatarAddrAndScoresWithRank.First(tuple =>
                        tuple.avatarAddr.Equals(currentAvatarAddr));
                    playerScore = playerTuple.score;
                }
                catch
                {
                    // Chain has not Score and GrandFinaleInfo about current avatar.
                    playerGrandFinaleParticipant = new PlayerGrandFinaleParticipant(
                        currentAvatarAddr,
                        BattleGrandFinale.DefaultScore,
                        0,
                        currentAvatar,
                        States.Instance.CurrentItemSlotStates[BattleType.Arena],
                        States.Instance.CurrentRuneSlotStates[BattleType.Arena],
                        States.Instance.GetEquippedRuneStates(BattleType.Arena),
                        default);
                    playerScore = playerGrandFinaleParticipant.Score;
                }
            }

            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            var runeIds = runeListSheet.Values.Select(x => x.Id).ToList();
            var addrBulk = avatarAddrAndScoresWithRank
                .SelectMany(tuple => new[]
                {
                    tuple.avatarAddr,
                    tuple.avatarAddr.Derive(LegacyInventoryKey),
                    ItemSlotState.DeriveAddress(tuple.avatarAddr, BattleType.Arena),
                    RuneSlotState.DeriveAddress(tuple.avatarAddr, BattleType.Arena),
                })
                .ToList();

            var playerGrandFinaleInfoAddr = GrandFinaleInformation.DeriveAddress(
                currentAvatarAddr,
                row.GrandFinaleId);
            if (isGrandFinaleParticipant)
            {
                addrBulk.Add(playerGrandFinaleInfoAddr);
            }

            var runeAddrBulk = new List<Address>();
            foreach (var tuple in avatarAddrAndScoresWithRank)
            {
                runeAddrBulk.AddRange(runeIds.Select(x => RuneState.DeriveAddress(tuple.avatarAddr, x)));
            }

            // NOTE: If the [`addrBulk`] is too large, and split and get separately.
            var runeStateBulk = await agent.GetStateBulk(runeAddrBulk);
            var stateBulk = await agent.GetStateBulk(addrBulk);
            var runeStates = new List<RuneState>();
            var result = avatarAddrAndScoresWithRank.Select(tuple =>
            {
                var (avatarAddr, score, rank) = tuple;
                AvatarState avatar = null;
                if (stateBulk[avatarAddr] is Dictionary avatarDict)
                {
                    try
                    {
                        avatar = new AvatarState(avatarDict);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"UpdateGrandFinaleParticipantsOrderedWithScoreAsync(): {e}");
                        avatar = null;
                    }
                }

                if (avatar is null)
                {
                    return null;
                }

                var inventory =
                    stateBulk[avatarAddr.Derive(LegacyInventoryKey)] is List inventoryList
                        ? new Model.Item.Inventory(inventoryList)
                        : null;
                avatar.inventory = inventory;

                var itemSlotState =
                    stateBulk[ItemSlotState.DeriveAddress(tuple.avatarAddr, BattleType.Arena)] is
                        List itemSlotList
                        ? new ItemSlotState(itemSlotList)
                        : new ItemSlotState(BattleType.Arena);

                var runeSlotState =
                    stateBulk[RuneSlotState.DeriveAddress(tuple.avatarAddr, BattleType.Arena)] is
                        List runeSlotList
                        ? new RuneSlotState(runeSlotList)
                        : new RuneSlotState(BattleType.Arena);

                runeStates.Clear();
                foreach (var id in runeIds)
                {
                    var address = RuneState.DeriveAddress(tuple.avatarAddr, id);
                    if (runeStateBulk[address] is List runeStateList)
                    {
                        runeStates.Add(new RuneState(runeStateList));
                    }
                }

                var equippedRuneStates = new List<RuneState>();
                foreach (var slot in runeSlotState.GetRuneSlot())
                {
                    if (!slot.RuneId.HasValue)
                    {
                        continue;
                    }

                    var runeState = runeStates.FirstOrDefault(x => x.RuneId == slot.RuneId);
                    if (runeState != null)
                    {
                        equippedRuneStates.Add(runeState);
                    }
                }

                return new GrandFinaleParticipant(
                    avatarAddr,
                    avatarAddr.Equals(currentAvatarAddr)
                        ? playerScore
                        : score,
                    rank,
                    avatar,
                    itemSlotState,
                    runeSlotState,
                    equippedRuneStates);
            }).Where(value => value is not null).ToArray();

            if (isGrandFinaleParticipant)
            {
                var playerGrandFinaleInfo = stateBulk[playerGrandFinaleInfoAddr] is List serialized
                    ? new GrandFinaleInformation(serialized)
                    : new GrandFinaleInformation(currentAvatarAddr, 1);
                if (playerGrandFinaleParticipant is null)
                {
                    var participant = result.FirstOrDefault(e =>
                        e.AvatarAddr.Equals(currentAvatarAddr));
                    playerGrandFinaleParticipant = new PlayerGrandFinaleParticipant(participant, playerGrandFinaleInfo);
                }
                else
                {
                    playerGrandFinaleParticipant.CurrentInfo = playerGrandFinaleInfo;
                }
            }

            GrandFinalePlayer = playerGrandFinaleParticipant;
            GrandFinaleParticipants = result;
            IsUpdating = false;
            return GrandFinaleParticipants;
        }

        private static (Address avatarAddr, int score, int rank)[] AddRank(
            (Address avatarAddr, int score)[] tuples, Address? currentAvatarAddr = null)
        {
            if (tuples.Length == 0)
            {
                return default;
            }

            var orderedTuples = tuples
                .OrderByDescending(tuple => tuple.score)
                .ThenByDescending(tuple => tuple.avatarAddr == currentAvatarAddr)
                .ThenBy(tuple => tuple.avatarAddr)
                .Select(tuple => (tuple.avatarAddr, tuple.score, 0))
                .ToArray();

            var result = new List<(Address avatarAddr, int score, int rank)>();
            var trunk = new List<(Address avatarAddr, int score, int rank)>();
            int? currentScore = null;
            var currentRank = 1;
            for (var i = 0; i < orderedTuples.Length; i++)
            {
                var tuple = orderedTuples[i];
                if (!currentScore.HasValue)
                {
                    currentScore = tuple.score;
                    trunk.Add(tuple);
                    continue;
                }

                if (currentScore.Value == tuple.score)
                {
                    trunk.Add(tuple);
                    currentRank++;
                    if (i < orderedTuples.Length - 1)
                    {
                        continue;
                    }

                    foreach (var tupleInTrunk in trunk)
                    {
                        result.Add((
                            tupleInTrunk.avatarAddr,
                            tupleInTrunk.score,
                            currentRank));
                    }

                    trunk.Clear();

                    continue;
                }

                foreach (var tupleInTrunk in trunk)
                {
                    result.Add((
                        tupleInTrunk.avatarAddr,
                        tupleInTrunk.score,
                        currentRank));
                }

                trunk.Clear();
                if (i < orderedTuples.Length - 1)
                {
                    trunk.Add(tuple);
                    currentScore = tuple.score;
                    currentRank++;
                    continue;
                }

                result.Add((
                    tuple.avatarAddr,
                    tuple.score,
                    currentRank + 1));
            }

            return result.ToArray();
        }
    }
}
