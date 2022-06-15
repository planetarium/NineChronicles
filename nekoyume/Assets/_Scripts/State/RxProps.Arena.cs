using System;
using System.Linq;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Arena;
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

        private static readonly ReactiveProperty<(long beginning, long end, long progress)>
            _arenaTicketProgress
                = new ReactiveProperty<(long beginning, long end, long progress)>();

        public static IReadOnlyReactiveProperty<(long beginning, long end, long progress)>
            ArenaTicketProgress => _arenaTicketProgress;

        // TODO!!!! Remove [`_arenaInfoTuple`] and use [`_playersArenaParticipant`] instead.
        private static readonly
            AsyncUpdatableRxProp<(ArenaInformation current, ArenaInformation next)>
            _arenaInfoTuple
                = new AsyncUpdatableRxProp<(ArenaInformation current, ArenaInformation next)>(
                    UpdateArenaInfoTupleAsync);

        private static long _arenaInfoTupleUpdatedBlockIndex;

        public static
            IReadOnlyAsyncUpdatableRxProp<(ArenaInformation current, ArenaInformation next)>
            ArenaInfoTuple => _arenaInfoTuple;

        private static readonly ReactiveProperty<PlayerArenaParticipant>
            _playersArenaParticipant
                = new ReactiveProperty<PlayerArenaParticipant>(null);

        public static IReadOnlyReactiveProperty<PlayerArenaParticipant>
            PlayersArenaParticipant => _playersArenaParticipant;

        private static readonly AsyncUpdatableRxProp<ArenaParticipant[]>
            _arenaParticipantsOrderedWithScore = new AsyncUpdatableRxProp<ArenaParticipant[]>(
                Array.Empty<ArenaParticipant>(),
                UpdateArenaParticipantsOrderedWithScoreAsync);

        private static long _arenaParticipantsOrderedWithScoreUpdatedBlockIndex;

        public static IReadOnlyAsyncUpdatableRxProp<ArenaParticipant[]>
            ArenaParticipantsOrderedWithScore => _arenaParticipantsOrderedWithScore;

        private static void StartArena()
        {
            // TODO!!!! Update [`_playersArenaParticipant`] when current avatar changed.
            // ReactiveAvatarState.Address
            //     .Subscribe(addr =>
            //     {
            //         if (_playersArenaParticipant.HasValue &&
            //             _playersArenaParticipant.Value.AvatarAddr == addr)
            //         {
            //             return;
            //         }
            //
            //         _playersArenaParticipant.Value = null;
            //     })
            //     .AddTo(_disposables);
        }

        private static void OnBlockIndexArena(long blockIndex)
        {
            UpdateArenaTicketProgress(blockIndex);
        }

        private static void UpdateArenaTicketProgress(
            long blockIndex)
        {
            var beginningBlockIndex = _states.WeeklyArenaState.ResetIndex;
            var endBlockIndex
                = beginningBlockIndex + _states.GameConfigState.DailyArenaInterval;
            var progressBlockIndex = blockIndex - beginningBlockIndex;
            _arenaTicketProgress.SetValueAndForceNotify((
                beginningBlockIndex,
                endBlockIndex,
                progressBlockIndex));
        }

        private static async UniTask<(ArenaInformation current, ArenaInformation next)>
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

            var blockIndex = Game.Game.instance.Agent.BlockIndex;
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
            var dict = await Game.Game.instance.Agent.GetStateBulk(
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

        private static async UniTask<ArenaParticipant[]>
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

            var blockIndex = Game.Game.instance.Agent.BlockIndex;
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
            var avatarAndScores = avatarAndScoreAddrList
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
            var avatarAndScoresWithRank = AddRank(avatarAndScores);
            PlayerArenaParticipant playersArenaParticipant = null;
            int playerScore;
            try
            {
                var playerTuple = avatarAndScoresWithRank.First(tuple =>
                    tuple.avatarAddr.Equals(currentAvatarAddr));
                playerScore = playerTuple.score;
                avatarAndScoresWithRank = GetBoundsWithPlayer(
                    avatarAndScoresWithRank,
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
                avatarAndScoresWithRank = GetBoundsWithPlayer(
                    avatarAndScoresWithRank,
                    currentRoundData.ArenaType,
                    playerScore);
            }

            var playerArenaInfoAddr = ArenaInformation.DeriveAddress(
                currentAvatarAddr,
                currentRoundData.ChampionshipId,
                currentRoundData.Round);
            var addrBulk = avatarAndScoresWithRank
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
            var result = avatarAndScoresWithRank.Select(tuple =>
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

        private static (Address avatarAddr, int score, int rank)[] AddRank(
            (Address avatarAddr, int score)[] tuples)
        {
            if (tuples.Length == 0)
            {
                return default;
            }

            var orderedTuples = tuples
                .OrderByDescending(tuple => tuple.score)
                .Select(tuple => (avatarAddr: tuple.avatarAddr, tuple.score, 0))
                .ToArray();
            var rank = 1;
            var cachedScore = orderedTuples[0].score;
            for (var i = 1; i < orderedTuples.Length; i++)
            {
                var tuple = orderedTuples[i];
                if (cachedScore == tuple.score)
                {
                    rank++;
                    continue;
                }

                for (var j = i - 1; j >= 0; j--)
                {
                    var previousTuple = orderedTuples[j];
                    if (previousTuple.score == cachedScore)
                    {
                        previousTuple.Item3 = rank;
                        continue;
                    }

                    break;
                }

                cachedScore = tuple.score;
            }

            return orderedTuples;
        }

        private static (Address avatarAddr, int score, int rank)[] GetBoundsWithPlayer(
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