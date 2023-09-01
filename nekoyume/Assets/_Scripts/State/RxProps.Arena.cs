using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Arena;
using Nekoyume.Game.LiveAsset;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;
using UnityEngine;
using static Lib9c.SerializeKeys;

namespace Nekoyume.State
{
    using UniRx;

    public static partial class RxProps
    {
        public class ArenaParticipant
        {
            public readonly Address AvatarAddr;
            public readonly int Score;
            public readonly int Rank;
            public readonly AvatarState AvatarState;
            public readonly ItemSlotState ItemSlotState;
            public readonly RuneSlotState RuneSlotState;
            public readonly List<RuneState> RuneStates;
            public readonly (int win, int lose) ExpectDeltaScore;

            public ArenaParticipant(
                Address avatarAddr,
                int score,
                int rank,
                AvatarState avatarState,
                ItemSlotState itemSlotState,
                RuneSlotState runeSlotState,
                List<RuneState> runeStates,
                (int win, int lose) expectDeltaScore)
            {
                AvatarAddr = avatarAddr;
                Score = score;
                Rank = rank;
                AvatarState = avatarState;
                ItemSlotState = itemSlotState;
                RuneSlotState = runeSlotState;
                RuneStates = runeStates;
                ExpectDeltaScore = expectDeltaScore;
            }

            public ArenaParticipant(ArenaParticipant value)
            {
                AvatarAddr = value.AvatarAddr;
                Score = value.Score;
                Rank = value.Rank;
                AvatarState = value.AvatarState;
                ItemSlotState = value.ItemSlotState;
                RuneSlotState = value.RuneSlotState;
                RuneStates = value.RuneStates;
                ExpectDeltaScore = value.ExpectDeltaScore;
            }
        }

        public class PlayerArenaParticipant : ArenaParticipant
        {
            public ArenaInformation CurrentArenaInfo;
            // public ArenaInformation NextArenaInfo;
            public int PurchasedCountDuringInterval;
            public long LastBattleBlockIndex;

            public PlayerArenaParticipant(
                Address avatarAddr,
                int score,
                int rank,
                AvatarState avatarState,
                ItemSlotState itemSlotState,
                RuneSlotState runeSlotState,
                List<RuneState> runeStates,
                (int win, int lose) expectDeltaScore,
                ArenaInformation currentArenaInfo,
                int purchasedCountDuringInterval,
                long lastBattleBlockIndex)
                : base(
                    avatarAddr,
                    score,
                    rank,
                    avatarState,
                    itemSlotState,
                    runeSlotState,
                    runeStates,
                    expectDeltaScore)
            {
                CurrentArenaInfo = currentArenaInfo;
                // NextArenaInfo = nextArenaInfo;
                PurchasedCountDuringInterval = purchasedCountDuringInterval;
                LastBattleBlockIndex = lastBattleBlockIndex;
            }

            public PlayerArenaParticipant(
                ArenaParticipant arenaParticipant,
                ArenaInformation currentArenaInfo,
                int purchasedCountDuringInterval,
                long lastBattleBlockIndex)
                : base(arenaParticipant)
            {
                CurrentArenaInfo = currentArenaInfo;
                // NextArenaInfo = nextArenaInfo;
                PurchasedCountDuringInterval = purchasedCountDuringInterval;
                LastBattleBlockIndex = lastBattleBlockIndex;
            }
        }

        // TODO!!!! Remove [`_arenaInfoTuple`] and use [`_playersArenaParticipant`] instead.
        private static readonly
            AsyncUpdatableRxProp<(ArenaInformation current, ArenaInformation next)>
            _arenaInfoTuple = new(UpdateArenaInfoTupleAsync);

        private static long _arenaInfoTupleUpdatedBlockIndex;

        public static
            IReadOnlyAsyncUpdatableRxProp<(ArenaInformation current, ArenaInformation next)>
            ArenaInfoTuple => _arenaInfoTuple;

        private static readonly ReactiveProperty<ArenaTicketProgress>
            _arenaTicketsProgress = new(new ArenaTicketProgress());

        public static IReadOnlyReactiveProperty<ArenaTicketProgress>
            ArenaTicketsProgress => _arenaTicketsProgress;

        private static readonly ReactiveProperty<PlayerArenaParticipant>
            _playersArenaParticipant = new(null);

        public static IReadOnlyReactiveProperty<PlayerArenaParticipant>
            PlayersArenaParticipant => _playersArenaParticipant;

        private static readonly AsyncUpdatableRxProp<ArenaParticipant[]>
            _arenaParticipantsOrderedWithScore = new(
                Array.Empty<ArenaParticipant>(),
                UpdateArenaParticipantsOrderedWithScoreAsync);

        private static long _arenaParticipantsOrderedWithScoreUpdatedBlockIndex;

        public static IReadOnlyAsyncUpdatableRxProp<ArenaParticipant[]>
            ArenaParticipantsOrderedWithScore => _arenaParticipantsOrderedWithScore;

        public static void UpdateArenaInfoToNext()
        {
            _arenaInfoTuple.Value = (_arenaInfoTuple.Value.next, null);
        }

        private static void StartArena()
        {
            OnBlockIndexArena(_agent.BlockIndex);
            OnAvatarChangedArena();

            ArenaInfoTuple
                .Subscribe(_ => UpdateArenaTicketProgress(_agent.BlockIndex))
                .AddTo(_disposables);

            PlayersArenaParticipant
                .Subscribe(_ => UpdateArenaTicketProgress(_agent.BlockIndex))
                .AddTo(_disposables);
        }

        private static void OnBlockIndexArena(long blockIndex)
        {
            UpdateArenaTicketProgress(blockIndex);
        }

        private static void OnAvatarChangedArena()
        {
            // NOTE: Reset all of cached block indexes for rx props when current avatar state changed.
            _arenaInfoTupleUpdatedBlockIndex = 0;
            _arenaParticipantsOrderedWithScoreUpdatedBlockIndex = 0;

            // TODO!!!! Update [`_playersArenaParticipant`] when current avatar changed.
            // if (_playersArenaParticipant.HasValue &&
            //     _playersArenaParticipant.Value.AvatarAddr == addr)
            // {
            //     return;
            // }
            //
            // _playersArenaParticipant.Value = null;
        }

        private static void UpdateArenaTicketProgress(long blockIndex)
        {
            const int maxTicketCount = ArenaInformation.MaxTicketCount;
            var ticketResetInterval = States.Instance.GameConfigState.DailyArenaInterval;
            var currentArenaInfo = _arenaInfoTuple.HasValue
                ? _arenaInfoTuple.Value.current
                : null;
            if (currentArenaInfo is null)
            {
                _arenaTicketsProgress.Value.Reset(maxTicketCount, maxTicketCount);
                _arenaTicketsProgress.SetValueAndForceNotify(_arenaTicketsProgress.Value);
                return;
            }

            var currentRoundData = _tableSheets.ArenaSheet.GetRoundByBlockIndex(blockIndex);
            var currentTicketCount = currentArenaInfo.GetTicketCount(
                blockIndex,
                currentRoundData.StartBlockIndex,
                ticketResetInterval);
            var purchasedCount = _playersArenaParticipant?.Value?.PurchasedCountDuringInterval ?? 0;
            var purchasedCountDuringInterval =  currentArenaInfo.GetPurchasedCountInInterval(
                blockIndex,
                currentRoundData.StartBlockIndex,
                ticketResetInterval,
                purchasedCount);
            var progressedBlockRange =
                (blockIndex - currentRoundData.StartBlockIndex) % ticketResetInterval;
            _arenaTicketsProgress.Value.Reset(
                currentTicketCount,
                maxTicketCount,
                (int)progressedBlockRange,
                ticketResetInterval,
                purchasedCountDuringInterval);
            _arenaTicketsProgress.SetValueAndForceNotify(
                _arenaTicketsProgress.Value);
        }

        private static async Task<(ArenaInformation current, ArenaInformation next)>
            UpdateArenaInfoTupleAsync(
                (ArenaInformation current, ArenaInformation next) previous)
        {
            var avatarAddress = _states.CurrentAvatarState?.address;
            if (!avatarAddress.HasValue)
            {
                return (null, null);
            }

            if (_arenaInfoTupleUpdatedBlockIndex == _agent.BlockIndex)
            {
                return previous;
            }

            _arenaInfoTupleUpdatedBlockIndex = _agent.BlockIndex;

            var blockIndex = _agent.BlockIndex;
            var sheet = _tableSheets.ArenaSheet;
            if (!sheet.TryGetCurrentRound(blockIndex, out var currentRoundData))
            {
                Debug.Log($"Failed to get current round data. block index({blockIndex})");
                return previous;
            }

            var currentArenaInfoAddress = ArenaInformation.DeriveAddress(
                avatarAddress.Value,
                currentRoundData.ChampionshipId,
                currentRoundData.Round);
            var nextArenaInfoAddress = sheet.TryGetNextRound(blockIndex, out var nextRoundData)
                ? ArenaInformation.DeriveAddress(
                    avatarAddress.Value,
                    nextRoundData.ChampionshipId,
                    nextRoundData.Round)
                : default;
            var dict = await _agent.GetStateBulkAsync(
                new[]
                {
                    currentArenaInfoAddress,
                    nextArenaInfoAddress
                }
            );
            var currentArenaInfo =
                dict[currentArenaInfoAddress] is List currentList
                    ? new ArenaInformation(currentList)
                    : null;
            var nextArenaInfo =
                dict[nextArenaInfoAddress] is List nextList
                    ? new ArenaInformation(nextList)
                    : null;
            return (currentArenaInfo, nextArenaInfo);
        }

        private static async Task<ArenaParticipant[]>
            UpdateArenaParticipantsOrderedWithScoreAsync(ArenaParticipant[] previous)
        {
            var avatarAddress = _states.CurrentAvatarState?.address;
            if (!avatarAddress.HasValue)
            {
                // TODO!!!!
                // [`States.CurrentAvatarState`]가 바뀔 때, 목록에 추가 정보를 업데이트 한다.
                return Array.Empty<ArenaParticipant>();
            }

            if (_arenaParticipantsOrderedWithScoreUpdatedBlockIndex == _agent.BlockIndex)
            {
                return previous;
            }

            _arenaParticipantsOrderedWithScoreUpdatedBlockIndex = _agent.BlockIndex;

            var blockIndex = _agent.BlockIndex;
            var currentAvatar = _states.CurrentAvatarState;
            var currentAvatarAddr = currentAvatar.address;
            var currentRoundData = _tableSheets.ArenaSheet.GetRoundByBlockIndex(blockIndex);
            var participantsAddr = ArenaParticipants.DeriveAddress(
                currentRoundData.ChampionshipId,
                currentRoundData.Round);
            var participants
                = await _agent.GetStateAsync(participantsAddr) is List participantsList
                    ? new ArenaParticipants(participantsList)
                    : null;
            if (participants is null)
            {
                Debug.Log($"Failed to get {nameof(ArenaParticipants)} with {participantsAddr.ToHex()}");

                // TODO!!!! [`_playersArenaParticipant`]를 이 문맥이 아닌 곳에서
                // 따로 처리합니다.
                _playersArenaParticipant.SetValueAndForceNotify(new PlayerArenaParticipant(
                    currentAvatarAddr,
                    ArenaScore.ArenaScoreDefault,
                    0,
                    currentAvatar,
                    States.Instance.CurrentItemSlotStates[BattleType.Arena],
                    States.Instance.CurrentRuneSlotStates[BattleType.Arena],
                    States.Instance.GetEquippedRuneStates(BattleType.Arena),
                    (0, 0),
                    new ArenaInformation(
                        currentAvatarAddr,
                        currentRoundData.ChampionshipId,
                        currentRoundData.Round),
                    0,
                    0));

                return Array.Empty<ArenaParticipant>();
            }

            var avatarAddrList = participants.AvatarAddresses;
            var avatarAndScoreAddrList = avatarAddrList
                .Select(avatarAddr => (
                    avatarAddr,
                    ArenaScore.DeriveAddress(
                        avatarAddr,
                        currentRoundData.ChampionshipId,
                        currentRoundData.Round)))
                .ToArray();
            // NOTE: If addresses is too large, and split and get separately.
            var scores = await _agent.GetStateBulkAsync(
                avatarAndScoreAddrList.Select(tuple => tuple.Item2));
            var avatarAddrAndScores = avatarAndScoreAddrList
                .Select(tuple =>
                {
                    var (avatarAddr, scoreAddr) = tuple;
                    return (
                        avatarAddr,
                        scores[scoreAddr] is List scoreList
                            ? (int)(Integer)scoreList[1]
                            : ArenaScore.ArenaScoreDefault
                    );
                })
                .ToArray();
            var avatarAddrAndScoresWithRank =
                AddRank(avatarAddrAndScores, currentAvatarAddr);
            PlayerArenaParticipant playersArenaParticipant = null;
            int playerScore;
            try
            {
                var playerTuple = avatarAddrAndScoresWithRank.First(tuple =>
                    tuple.avatarAddr.Equals(currentAvatarAddr));
                playerScore = playerTuple.score;
                avatarAddrAndScoresWithRank = GetBoundsWithPlayerScore(
                    avatarAddrAndScoresWithRank,
                    currentRoundData.ArenaType,
                    playerScore);
            }
            catch
            {
                playersArenaParticipant = new PlayerArenaParticipant(
                    currentAvatarAddr,
                    ArenaScore.ArenaScoreDefault,
                    0,
                    currentAvatar,
                    States.Instance.CurrentItemSlotStates[BattleType.Arena],
                    States.Instance.CurrentRuneSlotStates[BattleType.Arena],
                    States.Instance.GetEquippedRuneStates(BattleType.Arena),
                    default,
                    null,
                    0,
                    0);
                playerScore = playersArenaParticipant.Score;
                avatarAddrAndScoresWithRank = GetBoundsWithPlayerScore(
                    avatarAddrAndScoresWithRank,
                    currentRoundData.ArenaType,
                    playerScore);
            }

            var playerArenaInfoAddr = ArenaInformation.DeriveAddress(
                currentAvatarAddr,
                currentRoundData.ChampionshipId,
                currentRoundData.Round);
            var purchasedCountAddress =
                playerArenaInfoAddr.Derive(BattleArena.PurchasedCountKey);
            var arenaAvatarAddress =
                ArenaAvatarState.DeriveAddress(States.Instance.CurrentAvatarState.address);

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

            foreach (var tuple in avatarAddrAndScoresWithRank)
            {
                addrBulk.AddRange(runeIds.Select(x => RuneState.DeriveAddress(tuple.avatarAddr, x)));
            }

            addrBulk.Add(playerArenaInfoAddr);
            addrBulk.Add(purchasedCountAddress);
            addrBulk.Add(arenaAvatarAddress);

            // NOTE: If the [`addrBulk`] is too large, and split and get separately.
            var stateBulk = await _agent.GetStateBulkAsync(addrBulk);
            var runeStates = new List<RuneState>();
            var result = avatarAddrAndScoresWithRank.Select(tuple =>
            {
                var (avatarAddr, score, rank) = tuple;
                var avatar = stateBulk[avatarAddr] is Dictionary avatarDict
                    ? new AvatarState(avatarDict)
                    : null;
                var inventory =
                    stateBulk[avatarAddr.Derive(LegacyInventoryKey)] is List inventoryList
                        ? new Model.Item.Inventory(inventoryList)
                        : null;
                if (avatar is { })
                {
                    avatar.inventory = inventory;
                }

                var itemSlotState =
                    stateBulk[ItemSlotState.DeriveAddress(avatarAddr, BattleType.Arena)] is
                        List itemSlotList
                        ? new ItemSlotState(itemSlotList)
                        : new ItemSlotState(BattleType.Arena);

                var runeSlotState =
                    stateBulk[RuneSlotState.DeriveAddress(avatarAddr, BattleType.Arena)] is
                        List runeSlotList
                        ? new RuneSlotState(runeSlotList)
                        : new RuneSlotState(BattleType.Arena);

                runeStates.Clear();
                foreach (var id in runeIds)
                {
                    var address = RuneState.DeriveAddress(avatarAddr, id);
                    if (stateBulk[address] is List runeStateList)
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

                var (win, lose, _) =
                    ArenaHelper.GetScores(playerScore, score);
                return new ArenaParticipant(
                    avatarAddr,
                    avatarAddr.Equals(currentAvatarAddr)
                        ? playerScore
                        : score,
                    rank,
                    avatar,
                    itemSlotState,
                    runeSlotState,
                    equippedRuneStates,
                    (win, lose)
                );
            }).ToArray();

            var playerArenaInfo = stateBulk[playerArenaInfoAddr] is List arenaInfoList
                ? new ArenaInformation(arenaInfoList)
                : null;
            var purchasedCountDuringInterval = 0;
            long lastBattleBlockIndex = 0;
            try
            {
                purchasedCountDuringInterval = stateBulk[purchasedCountAddress] is Integer iValue
                    ? iValue
                    : 0;

                var arenaAvatarState = stateBulk[arenaAvatarAddress] is List iValue2
                    ? new ArenaAvatarState(iValue2)
                    : null;
                lastBattleBlockIndex = arenaAvatarState?.LastBattleBlockIndex ?? 0;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            // NOTE: There is players `ArenaParticipant` in chain.
            if (playersArenaParticipant is null)
            {
                var ap = result.FirstOrDefault(e =>
                    e.AvatarAddr.Equals(currentAvatarAddr));
                playersArenaParticipant = new PlayerArenaParticipant(
                    ap,
                    playerArenaInfo,
                    purchasedCountDuringInterval,
                    lastBattleBlockIndex);
            }
            else
            {
                playersArenaParticipant.CurrentArenaInfo = playerArenaInfo;
                playersArenaParticipant.PurchasedCountDuringInterval = purchasedCountDuringInterval;
            }

            _playersArenaParticipant.SetValueAndForceNotify(playersArenaParticipant);

            return result;
        }

        public static (Address avatarAddr, int score, int rank)[] AddRank(
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

        private static (Address avatarAddr, int score, int rank)[] GetBoundsWithPlayerScore(
            (Address avatarAddr, int score, int rank)[] tuples,
            ArenaType arenaType,
            int playerScore)
        {
            var bounds = ArenaHelper.ScoreLimits.ContainsKey(arenaType)
                ? ArenaHelper.ScoreLimits[arenaType]
                : ArenaHelper.ScoreLimits.First().Value;

            bounds = (bounds.upper + playerScore, bounds.lower + playerScore);
            return tuples
                .Where(tuple => tuple.score <= bounds.upper && tuple.score >= bounds.lower)
                .ToArray();
        }
    }
}
