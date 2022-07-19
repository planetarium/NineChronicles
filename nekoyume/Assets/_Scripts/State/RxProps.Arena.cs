using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Arena;
using Nekoyume.Helper;
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
            public readonly (int win, int lose) ExpectDeltaScore;

            public readonly int CP;

            public ArenaParticipant(
                Address avatarAddr,
                int score,
                int rank,
                AvatarState avatarState,
                (int win, int lose) expectDeltaScore)
            {
                AvatarAddr = avatarAddr;
                Score = score;
                Rank = rank;
                AvatarState = avatarState;
                ExpectDeltaScore = expectDeltaScore;

                CP = AvatarState?.GetCP() ?? 0;
            }

            public ArenaParticipant(ArenaParticipant value)
            {
                AvatarAddr = value.AvatarAddr;
                Score = value.Score;
                Rank = value.Rank;
                AvatarState = value.AvatarState;
                ExpectDeltaScore = value.ExpectDeltaScore;

                CP = AvatarState?.GetCP() ?? 0;
            }
        }

        public class PlayerArenaParticipant : ArenaParticipant
        {
            public ArenaInformation CurrentArenaInfo;
            // public ArenaInformation NextArenaInfo;

            public PlayerArenaParticipant(
                Address avatarAddr,
                int score,
                int rank,
                AvatarState avatarState,
                (int win, int lose) expectDeltaScore,
                ArenaInformation currentArenaInfo)
                : base(
                    avatarAddr,
                    score,
                    rank,
                    avatarState,
                    expectDeltaScore)
            {
                CurrentArenaInfo = currentArenaInfo;
                // NextArenaInfo = nextArenaInfo;
            }

            public PlayerArenaParticipant(
                ArenaParticipant arenaParticipant,
                ArenaInformation currentArenaInfo)
                : base(arenaParticipant)
            {
                CurrentArenaInfo = currentArenaInfo;
                // NextArenaInfo = nextArenaInfo;
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

        private static readonly ReactiveProperty<(
            int currentTicketCount,
            int maxTicketCount,
            int progressedBlockRange,
            int totalBlockRange,
            string remainTimespanToReset)> _arenaTicketProgress = new();

        public static IReadOnlyReactiveProperty<(
            int currentTicketCount,
            int maxTicketCount,
            int progressedBlockRange,
            int totalBlockRange,
            string remainTimespanToReset)> ArenaTicketProgress =>
            _arenaTicketProgress;

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
            ArenaInfoTuple
                .Subscribe(_ => UpdateArenaTicketProgress(_agent.BlockIndex))
                .AddTo(_disposables);

            OnBlockIndex(_agent.BlockIndex);
            OnAvatarChangedArena();
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
            var ticketResetInterval =
                States.Instance.GameConfigState.DailyArenaInterval;
            var currentArenaInfo = _arenaInfoTuple.HasValue
                ? _arenaInfoTuple.Value.current
                : null;
            if (currentArenaInfo is null)
            {
                _arenaTicketProgress.SetValueAndForceNotify((
                    maxTicketCount,
                    maxTicketCount,
                    0,
                    0,
                    ""));
                return;
            }

            var currentRoundData = _tableSheets.ArenaSheet.GetRoundByBlockIndex(blockIndex);
            var currentTicketCount = currentArenaInfo.GetTicketCount(
                blockIndex,
                currentRoundData.StartBlockIndex,
                ticketResetInterval);
            var progressedBlockRange =
                (blockIndex - currentRoundData.StartBlockIndex) % ticketResetInterval;
            _arenaTicketProgress.SetValueAndForceNotify((
                currentTicketCount,
                maxTicketCount,
                (int)progressedBlockRange,
                ticketResetInterval,
                Util.GetBlockToTime(ticketResetInterval - progressedBlockRange)));
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
            var dict = await _agent.GetStateBulk(
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
                var arenaAvatarState = currentAvatar.ToArenaAvatarState();
                var clonedCurrentAvatar = currentAvatar.CloneAndApplyToInventory(arenaAvatarState);
                _playersArenaParticipant.SetValueAndForceNotify(new PlayerArenaParticipant(
                    currentAvatarAddr,
                    ArenaScore.ArenaScoreDefault,
                    0,
                    clonedCurrentAvatar,
                    (0, 0),
                    new ArenaInformation(
                        currentAvatarAddr,
                        currentRoundData.ChampionshipId,
                        currentRoundData.Round)));

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
            var scores = await _agent.GetStateBulk(
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
                AddRank(avatarAddrAndScores);
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
                var arenaAvatarState = currentAvatar.ToArenaAvatarState();
                var clonedCurrentAvatar = currentAvatar.CloneAndApplyToInventory(arenaAvatarState);
                playersArenaParticipant = new PlayerArenaParticipant(
                    currentAvatarAddr,
                    ArenaScore.ArenaScoreDefault,
                    0,
                    clonedCurrentAvatar,
                    default,
                    null);
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
            var addrBulk = avatarAddrAndScoresWithRank
                .SelectMany(tuple => new[]
                {
                    tuple.avatarAddr,
                    tuple.avatarAddr.Derive(LegacyInventoryKey),
                    ArenaAvatarState.DeriveAddress(tuple.avatarAddr),
                })
                .ToList();
            addrBulk.Add(playerArenaInfoAddr);
            // NOTE: If the [`addrBulk`] is too large, and split and get separately.
            var stateBulk = await _agent.GetStateBulk(addrBulk);
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

                var arenaAvatar =
                    stateBulk[ArenaAvatarState.DeriveAddress(avatarAddr)] is List arenaAvatarList
                        ? new ArenaAvatarState(arenaAvatarList)
                        : null;
                avatar = avatar.ApplyToInventory(arenaAvatar);
                var (win, lose, _) =
                    ArenaHelper.GetScores(playerScore, score);
                return new ArenaParticipant(
                    avatarAddr,
                    avatarAddr.Equals(currentAvatarAddr)
                        ? playerScore
                        : score,
                    rank,
                    avatar,
                    (win, lose)
                );
            }).ToArray();

            var playerArenaInfo = stateBulk[playerArenaInfoAddr] is List arenaInfoList
                ? new ArenaInformation(arenaInfoList)
                : null;
            // NOTE: There is players `ArenaParticipant` in chain.
            if (playersArenaParticipant is null)
            {
                var ap = result.FirstOrDefault(e =>
                    e.AvatarAddr.Equals(currentAvatarAddr));
                playersArenaParticipant = new PlayerArenaParticipant(ap, playerArenaInfo);
            }
            else
            {
                playersArenaParticipant.CurrentArenaInfo = playerArenaInfo;
            }

            _playersArenaParticipant.SetValueAndForceNotify(playersArenaParticipant);

            return result;
        }

        public static (Address avatarAddr, int score, int rank)[] AddRank(
            (Address avatarAddr, int score)[] tuples)
        {
            if (tuples.Length == 0)
            {
                return default;
            }

            var orderedTuples = tuples
                .OrderByDescending(tuple => tuple.score)
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
            switch (arenaType)
            {
                case ArenaType.OffSeason:
                    return tuples;
                case ArenaType.Season:
                case ArenaType.Championship:
                    var bounds = ArenaHelper.ScoreLimits[arenaType];
                    bounds = (bounds.Item1 + playerScore, bounds.Item2 + playerScore);
                    return tuples
                        .Where(tuple =>
                            tuple.score <= bounds.Item1 &&
                            tuple.score >= bounds.Item2)
                        .ToArray();
                default:
                    throw new ArgumentOutOfRangeException(nameof(arenaType), arenaType, null);
            }
        }
    }
}
