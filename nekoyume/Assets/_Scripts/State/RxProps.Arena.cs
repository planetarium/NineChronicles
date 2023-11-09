using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Arena;
using Nekoyume.Game.LiveAsset;
using Nekoyume.GraphQL;
using Nekoyume.Helper;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;
using Nekoyume.UI.Model;
using UnityEngine;
using static Lib9c.SerializeKeys;

namespace Nekoyume.State
{
    using UniRx;

    public static partial class RxProps
    {
        // TODO!!!! Remove [`_arenaInfoTuple`] and use [`_playersArenaParticipant`] instead.
        private static readonly
            AsyncUpdatableRxProp<(ArenaInformation current, ArenaInformation next)>
            _arenaInfoTuple = new(UpdateArenaInfoTupleAsync);

        private static long _arenaInfoTupleUpdatedBlockIndex;

        private static readonly ReactiveProperty<ArenaParticipantModel> _playerArenaInfo =
            new(null);

        private static readonly ReactiveProperty<int> _purchasedDuringInterval = new();

        public static IReadOnlyReactiveProperty<int> PurchasedDuringInterval =>
            _purchasedDuringInterval;
        private static readonly ReactiveProperty<long> _lastBattleBlockIndex = new();

        public static IReadOnlyReactiveProperty<long> LastBattleBlockIndex =>
            _lastBattleBlockIndex;
        public static IReadOnlyReactiveProperty<ArenaParticipantModel> PlayerArenaInfo =>
            _playerArenaInfo;
        public static
            IReadOnlyAsyncUpdatableRxProp<(ArenaInformation current, ArenaInformation next)>
            ArenaInfoTuple => _arenaInfoTuple;

        private static readonly ReactiveProperty<ArenaTicketProgress>
            _arenaTicketsProgress = new(new ArenaTicketProgress());

        public static IReadOnlyReactiveProperty<ArenaTicketProgress>
            ArenaTicketsProgress => _arenaTicketsProgress;

        private static long _arenaParticipantsOrderedWithScoreUpdatedBlockIndex;

        private static readonly AsyncUpdatableRxProp<List<ArenaParticipantModel>>
            _arenaInformationOrderedWithScore = new(
                new List<ArenaParticipantModel>(),
                UpdateArenaInformationOrderedWithScoreAsync);

        public static IReadOnlyAsyncUpdatableRxProp<List<ArenaParticipantModel>>
            ArenaInformationOrderedWithScore => _arenaInformationOrderedWithScore;

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

            PlayerArenaInfo
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
            var purchasedCount = PurchasedDuringInterval.Value;
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

        private static async Task<List<ArenaParticipantModel>>
            UpdateArenaInformationOrderedWithScoreAsync(List<ArenaParticipantModel> previous)
        {
            var avatarAddress = _states.CurrentAvatarState?.address;
            List<ArenaParticipantModel> avatarAddrAndScoresWithRank =
                new List<ArenaParticipantModel>();
            if (!avatarAddress.HasValue)
            {
                // TODO!!!!
                // [`States.CurrentAvatarState`]가 바뀔 때, 목록에 추가 정보를 업데이트 한다.
                return avatarAddrAndScoresWithRank;
            }

            if (_arenaParticipantsOrderedWithScoreUpdatedBlockIndex == _agent.BlockIndex)
            {
                return previous;
            }

            _arenaParticipantsOrderedWithScoreUpdatedBlockIndex = _agent.BlockIndex;

            var currentAvatar = _states.CurrentAvatarState;
            var currentAvatarAddr = currentAvatar.address;
            var arenaInfo = new List<ArenaParticipantModel>();
            int purchasedCountDuringInterval = 0;
            long lastBattleBlockIndex = 0L;
            int playerWinScore = 0;
            int playerLoseScore = 0;
            try
            {
                // var client = new NineChroniclesAPIClient("http://localhost:50000/graphql");
                // var response = await client.QueryArenaInfoAsync(currentAvatarAddr);
                var response = await Game.Game.instance.RpcClient.QueryArenaInfoAsync(currentAvatarAddr);
                var graphqlData = response.StateQuery.ArenaInfo;
                arenaInfo = graphqlData.ArenaParticipants;
                purchasedCountDuringInterval = graphqlData.PurchasedCountDuringInterval;
                lastBattleBlockIndex = graphqlData.LastBattleBlockIndex;
                var arenaInformationModel = graphqlData.ArenaInformation;
                playerWinScore = arenaInformationModel.Win;
                playerLoseScore = arenaInformationModel.Lose;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            ArenaParticipantModel playerArenaInf = new ArenaParticipantModel
            {
                AvatarAddr = currentAvatarAddr,
                Score = ArenaScore.ArenaScoreDefault,
                WinScore = 0,
                LoseScore = 0,
                NameWithHash = currentAvatar.NameWithHash,
                PortraitId = Util.GetPortraitId(BattleType.Arena),
                Cp = Util.TotalCP(BattleType.Arena),
                Level = currentAvatar.level
            };

            if (!arenaInfo.Any())
            {
                Debug.Log($"Failed to get {nameof(ArenaParticipantModel)}");

                // TODO!!!! [`_playersArenaParticipant`]를 이 문맥이 아닌 곳에서
                // 따로 처리합니다.
                _playerArenaInfo.SetValueAndForceNotify(playerArenaInf);
                return avatarAddrAndScoresWithRank;
            }

            var playerArenaInfo = arenaInfo.FirstOrDefault(p => p.AvatarAddr == currentAvatarAddr);
            if (playerArenaInfo is { })
            {
                playerArenaInfo.WinScore = playerWinScore;
                playerArenaInfo.LoseScore = playerLoseScore;
            }
            else
            {
                playerArenaInf.Rank = arenaInfo.Max(r => r.Rank);
                playerArenaInfo = playerArenaInf;
                arenaInfo.Add(playerArenaInfo);
            }
            _playerArenaInfo.SetValueAndForceNotify(playerArenaInfo);
            // NOTE: If the [`addrBulk`] is too large, and split and get separately.
            _purchasedDuringInterval.SetValueAndForceNotify(purchasedCountDuringInterval);
            _lastBattleBlockIndex.SetValueAndForceNotify(lastBattleBlockIndex);

            return arenaInfo;
        }
    }
}
