using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.ApiClient;
using Nekoyume.GraphQL;
using Nekoyume.Helper;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;
using Nekoyume.UI.Model;
using GeneratedApiNamespace.ArenaServiceClient;

namespace Nekoyume.State
{
    using Libplanet.Common;
    using Libplanet.Types.Tx;
    using System.Reactive.Linq;
    using System.Security.Cryptography;
    using UniRx;

    public static partial class RxProps
    {
        #region RxPropInternal
        // TODO!!!! Remove [`_arenaInfoTuple`] and use [`_playersArenaParticipant`] instead.
        private static readonly
            AsyncUpdatableRxProp<(ArenaInformation current, ArenaInformation next)>
            _arenaInfoTuple = new(UpdateArenaInfoTupleAsync);
        private static readonly AsyncUpdatableRxProp<List<ArenaParticipantModel>>
            _arenaInformationOrderedWithScore = new(
                new List<ArenaParticipantModel>(),
                UpdateArenaInformationOrderedWithScoreAsync);
        private static readonly ReactiveProperty<ArenaParticipantModel> _playerArenaInfo =
            new(null);
        private static readonly ReactiveProperty<int> _purchasedDuringInterval = new();
        private static readonly ReactiveProperty<long> _lastArenaBattleBlockIndex = new();
        private static readonly ReactiveProperty<ArenaTicketProgress>
            _arenaTicketsProgress = new(new ArenaTicketProgress());
        
        private static readonly ReactiveProperty<List<SeasonResponse>> _arenaSeasonResponses = new(new List<SeasonResponse>());
        #endregion RxPropInternal

        #region RxPropObservable
        public static IReadOnlyReactiveProperty<int> PurchasedDuringInterval => _purchasedDuringInterval;
        public static IReadOnlyReactiveProperty<long> LastArenaBattleBlockIndex => _lastArenaBattleBlockIndex;
        public static IReadOnlyReactiveProperty<ArenaParticipantModel> PlayerArenaInfo => _playerArenaInfo;

        public static IReadOnlyAsyncUpdatableRxProp<List<ArenaParticipantModel>>
            ArenaInformationOrderedWithScore => _arenaInformationOrderedWithScore;
        public static IReadOnlyReactiveProperty<ArenaTicketProgress>
            ArenaTicketsProgress => _arenaTicketsProgress;
        public static IReadOnlyAsyncUpdatableRxProp<(ArenaInformation current, ArenaInformation next)>
            ArenaInfoTuple => _arenaInfoTuple;

        public static IReadOnlyReactiveProperty<List<SeasonResponse>> ArenaSeasonResponses => _arenaSeasonResponses;
        #endregion RxPropObservable

        private static long _arenaParticipantsOrderedWithScoreUpdatedBlockIndex;
        private static long _arenaInfoTupleUpdatedBlockIndex;
        private static int _currentSeasonId;
        private static int _lastBattleLogId;

        public static int CurrentArenaSeasonId
        {
            get => _currentSeasonId;
            private set => _currentSeasonId = value;
        }

        public static int LastBattleLogId
        {
            get => _lastBattleLogId;
            set => _lastBattleLogId = value;
        }
        
        public static List<int> GetSeasonNumbersOfChampionship(){
            return ArenaSeasonResponses.Value
                    .Where(seasonResponse => seasonResponse.ArenaType == GeneratedApiNamespace.ArenaServiceClient.ArenaType.SEASON)
                    .Select(seasonResponse => seasonResponse.Id)
                    .ToList();
        }

        public static SeasonResponse GetSeasonResponseByBlockIndex(long blockIndex){
            return ArenaSeasonResponses.Value.Where(seasonResponse => seasonResponse.StartBlockIndex< blockIndex && blockIndex <= seasonResponse.EndBlockIndex).FirstOrDefault();
        }

        public static SeasonResponse GetNextSeasonResponseByBlockIndex(long blockIndex)
        {
            return ArenaSeasonResponses.Value
                .Where(seasonResponse => seasonResponse.StartBlockIndex > blockIndex)
                .OrderBy(seasonResponse => seasonResponse.StartBlockIndex)
                .FirstOrDefault();
        }

        public static void UpdateArenaInfoToNext()
        {
            _arenaInfoTuple.Value = (_arenaInfoTuple.Value.next, null);
        }

        private static bool _isUpdatingSeasonResponses = false;
        public static async UniTask UpdateSeasonResponsesAsync(long blockIndex)
        {
            if (_isUpdatingSeasonResponses) return;

            _isUpdatingSeasonResponses = true;

            await ApiClients.Instance.Arenaservicemanager.Client.GetSeasonsClassifybychampionshipAsync(blockIndex,
                on200OK: response =>
                {
                    _arenaSeasonResponses.SetValueAndForceNotify(response.OrderBy(season => season.StartBlockIndex).ToList());
                    _isUpdatingSeasonResponses = false;
                },
                onError: error =>
                {
                    // Handle error case
                    NcDebug.LogError($"Error fetching seasons: {error}");
                    _isUpdatingSeasonResponses = false;
                });
        }

        public static async UniTask<string> ArenaPostCurrentSeasonsParticipantsAsync()
        {
            var currentAvatar = _states.CurrentAvatarState;
            var currentAvatarAddr = currentAvatar.address;
            var portraitId = Util.GetPortraitId(BattleType.Arena);
            var cp = Util.TotalCP(BattleType.Arena);
            return await ApiClients.Instance.Arenaservicemanager.PostUsersAsync(currentAvatarAddr.ToString(), currentAvatar.NameWithHash, portraitId, cp, currentAvatar.level);
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
            var purchasedCountDuringInterval = currentArenaInfo.GetPurchasedCountInInterval(
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
                (ArenaInformation current, ArenaInformation next) previous, HashDigest<SHA256> stateRootHash)
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

            try
            {
                var currentSeason = await ApiClients.Instance.Arenaservicemanager.GetSeasonByBlockAsync(_arenaInfoTupleUpdatedBlockIndex);
                _currentSeasonId = currentSeason.Id;
            }
            catch (Exception e)
            {
                NcDebug.LogError($"Failed to get current season: {e}");
                return (null, null);
            }

            var blockIndex = _agent.BlockIndex;
            var sheet = _tableSheets.ArenaSheet;
            if (!sheet.TryGetCurrentRound(blockIndex, out var currentRoundData))
            {
                NcDebug.Log($"Failed to get current round data. block index({blockIndex})");
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
                stateRootHash,
                ReservedAddresses.LegacyAccount,
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
            UpdateArenaInformationOrderedWithScoreAsync(
                List<ArenaParticipantModel> previous, HashDigest<SHA256> stateRootHash)
        {
            var avatarAddress = _states.CurrentAvatarState?.address;
            var avatarAddrAndScoresWithRank =
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

            // TODO: 신규아레나
            // var lastBattleBlockIndex = arenaAvatarState?.LastBattleBlockIndex ?? 0L;
            try
            {
                arenaInfo = await ApiClients.Instance.Arenaservicemanager.GetAvailableopponentsAsync(_currentSeasonId, currentAvatarAddr.ToString());
            }
            catch (Exception e)
            {
                NcDebug.LogException(e);
                // TODO: this is temporary code for local testing.
                arenaInfo.AddRange(_states.AvatarStates.Values.Select(avatar => new ArenaParticipantModel
                {
                    AvatarAddr = avatar.address,
                    NameWithHash = avatar.NameWithHash
                }));
            }

            string playerGuildName = null;
            if (Game.Game.instance.GuildModels.Any())
            {
                var guildModels = Game.Game.instance.GuildModels;
                foreach (var guildModel in guildModels)
                {
                    if (guildModel.AvatarModels.Any(a => a.AvatarAddress == currentAvatarAddr))
                    {
                        playerGuildName = guildModel.Name;
                    }

                    foreach (var info in arenaInfo)
                    {
                        var model = guildModel.AvatarModels.FirstOrDefault(a => a.AvatarAddress == info.AvatarAddr);
                        if (model is not null)
                        {
                            info.GuildName = guildModel.Name;
                        }
                    }
                }
            }

            var portraitId = Util.GetPortraitId(BattleType.Arena);
            var cp = Util.TotalCP(BattleType.Arena);
            var playerArenaInf = new ArenaParticipantModel
            {
                AvatarAddr = currentAvatarAddr,
                Score = ArenaScore.ArenaScoreDefault,
                WinScore = 0,
                LoseScore = 0,
                NameWithHash = currentAvatar.NameWithHash,
                PortraitId = portraitId,
                Cp = cp,
                Level = currentAvatar.level,
                GuildName = playerGuildName
            };

            if (!arenaInfo.Any())
            {
                NcDebug.Log($"Failed to get {nameof(ArenaParticipantModel)}");

                _playerArenaInfo.SetValueAndForceNotify(playerArenaInf);
                return avatarAddrAndScoresWithRank;
            }

            var playerArenaInfo = await ApiClients.Instance.Arenaservicemanager.GetSeasonsLeaderboardParticipantAsync(_currentSeasonId, currentAvatarAddr.ToString());

            if (playerArenaInfo is null)
            {
                var maxRank = arenaInfo.Max(r => r.Rank);
                var firstMaxRankIndex = arenaInfo.FindIndex(info => info.Rank == maxRank);
                playerArenaInf.Rank = maxRank;
                playerArenaInfo = playerArenaInf;
                arenaInfo.Insert(firstMaxRankIndex, playerArenaInfo);
            }
            else
            {
                playerArenaInfo.PortraitId = portraitId;
            }

            // TODO: 신규아레나
            // _lastArenaBattleBlockIndex.SetValueAndForceNotify(lastBattleBlockIndex);
            // _purchasedDuringInterval.SetValueAndForceNotify(purchasedCountDuringInterval);

            SetArenaInfoOnMainThreadAsync(playerArenaInfo).Forget();
            return arenaInfo;
        }

        private static async UniTask SetArenaInfoOnMainThreadAsync(ArenaParticipantModel playerArenaInfo)
        {
            await UniTask.SwitchToMainThread();
            _playerArenaInfo.SetValueAndForceNotify(playerArenaInfo);
        }
    }
}
