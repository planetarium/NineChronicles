using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet.Action.State;
using Nekoyume.ApiClient;
using Nekoyume.Helper;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;
using Nekoyume.UI.Model;
using GeneratedApiNamespace.ArenaServiceClient;

namespace Nekoyume.State
{
    using Libplanet.Common;
    using System.Reactive.Linq;
    using System.Security.Cryptography;
    using UniRx;

    public static partial class RxProps
    {
        #region RxPropInternal
        // TODO!!!! Remove [`_arenaInfoTuple`] and use [`_playersArenaParticipant`] instead.
        private static readonly
            AsyncUpdatableRxProp<ArenaInfoResponse>
            _arenaInfo = new(UpdateArenaInfoAsync);
        private static readonly AsyncUpdatableRxProp<List<ArenaParticipantModel>>
            _arenaInformationOrderedWithScore = new(
                new List<ArenaParticipantModel>(),
                UpdateArenaInformationOrderedWithScoreAsync);
    
        private static readonly ReactiveProperty<int> _purchasedDuringInterval = new();
        private static readonly ReactiveProperty<long> _lastArenaBattleBlockIndex = new();
        private static readonly ReactiveProperty<ArenaTicketProgress>
            _arenaTicketsProgress = new(new ArenaTicketProgress());

        private static readonly ReactiveProperty<List<SeasonResponse>> _arenaSeasonResponses = new(new List<SeasonResponse>());
        #endregion RxPropInternal

        #region RxPropObservable
        public static IReadOnlyReactiveProperty<int> PurchasedDuringInterval => _purchasedDuringInterval;
        public static IReadOnlyReactiveProperty<long> LastArenaBattleBlockIndex => _lastArenaBattleBlockIndex;

        public static IReadOnlyAsyncUpdatableRxProp<List<ArenaParticipantModel>>
            ArenaInformationOrderedWithScore => _arenaInformationOrderedWithScore;
        public static IReadOnlyReactiveProperty<ArenaTicketProgress>
            ArenaTicketsProgress => _arenaTicketsProgress;
        public static IReadOnlyAsyncUpdatableRxProp<ArenaInfoResponse>
            ArenaInfo => _arenaInfo;

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

        public static int LastBattleId
        {
            get => _lastBattleLogId;
            set => _lastBattleLogId = value;
        }

        public static string OperationAccountAddress;

        public static List<int> GetSeasonNumbersOfChampionship()
        {
            return ArenaSeasonResponses.Value
                    .Where(seasonResponse => seasonResponse.ArenaType == GeneratedApiNamespace.ArenaServiceClient.ArenaType.SEASON)
                    .Select(seasonResponse => seasonResponse.Id)
                    .ToList();
        }

        public static SeasonResponse GetSeasonResponseByBlockIndex(long blockIndex)
        {
            return ArenaSeasonResponses.Value.Where(seasonResponse => seasonResponse.StartBlockIndex < blockIndex && blockIndex <= seasonResponse.EndBlockIndex).FirstOrDefault();
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
            // todo : 아레나 서비스
            // 시즌종료시 처리
        }

        private static bool _isUpdatingSeasonResponses = false;
        public static async UniTask UpdateSeasonResponsesAsync(long blockIndex)
        {
            if (_isUpdatingSeasonResponses) return;

            if (_arenaSeasonResponses.Value.Count != 0 && _arenaSeasonResponses.Value.Last().EndBlockIndex > blockIndex) return;

            _isUpdatingSeasonResponses = true;

            await ApiClients.Instance.Arenaservicemanager.Client.GetSeasonsClassifybychampionshipAsync(blockIndex,
                on200OK: response =>
                {
                    _arenaSeasonResponses.SetValueAndForceNotify(response.Seasons.OrderBy(season => season.StartBlockIndex).ToList());
                    OperationAccountAddress = response.OperationAccountAddress;
                    _isUpdatingSeasonResponses = false;
                },
                onError: error =>
                {
                    // Handle error case
                    NcDebug.LogError($"Error fetching seasons: {error}");
                    _isUpdatingSeasonResponses = false;
                });
        }

        public static async UniTask<string> PostUserAsync()
        {
            var currentAvatar = _states.CurrentAvatarState;
            var currentAvatarAddr = currentAvatar.address;
            var portraitId = Util.GetPortraitId(BattleType.Arena);
            var cp = Util.TotalCP(BattleType.Arena);
            return await ApiClients.Instance.Arenaservicemanager.PostUsersAsync(currentAvatarAddr.ToString(), currentAvatar.NameWithHash, portraitId, cp, currentAvatar.level);
        }

        private static void StartArena()
        {
            OnAvatarChangedArena();

            // ArenaInfo
            //     .Subscribe(_ => UpdateArenaTicketProgress(_agent.BlockIndex))
            //     .AddTo(_disposables);

            // PlayerArenaInfo
            //     .Subscribe(_ => UpdateArenaTicketProgress(_agent.BlockIndex))
            //     .AddTo(_disposables);
        }

        private static void OnAvatarChangedArena()
        {
            // NOTE: Reset all of cached block indexes for rx props when current avatar state changed.
            _arenaInfoTupleUpdatedBlockIndex = 0;
            _arenaParticipantsOrderedWithScoreUpdatedBlockIndex = 0;
            PostUserAsync().Forget();
        }

        public static void UpdateArenaTicketProgress(long blockIndex)
        {
            var currentSeason = GetSeasonResponseByBlockIndex(blockIndex);
            int maxTicketCount = currentSeason.BattleTicketPolicy.DefaultTicketsPerRound;
            var ticketResetInterval = currentSeason.RoundInterval;
            var currentArenaInfo = _arenaInfo.HasValue
                ? _arenaInfo.Value
                : null;
            if (currentArenaInfo is null)
            {
                _arenaTicketsProgress.Value.Reset(maxTicketCount, maxTicketCount);
                _arenaTicketsProgress.SetValueAndForceNotify(_arenaTicketsProgress.Value);
                return;
            }
            var currentTicketCount = currentArenaInfo.BattleTicketStatus.RemainingTicketsPerRound;
            var progressedBlockRange =
                (blockIndex - currentSeason.StartBlockIndex) % ticketResetInterval;
            _arenaTicketsProgress.Value.Reset(
                currentTicketCount,
                maxTicketCount,
                (int)progressedBlockRange,
                ticketResetInterval,
                currentArenaInfo.BattleTicketStatus.TicketsPurchasedPerRound);
            _arenaTicketsProgress.SetValueAndForceNotify(
                _arenaTicketsProgress.Value);
        }

        private static async Task<ArenaInfoResponse>
            UpdateArenaInfoAsync(
                ArenaInfoResponse arenaInfo, HashDigest<SHA256> stateRootHash)
        {
            var avatarAddress = _states.CurrentAvatarState?.address;
            if (!avatarAddress.HasValue)
            {
                return null;
            }

            if (_arenaInfoTupleUpdatedBlockIndex == _agent.BlockIndex)
            {
                return arenaInfo;
            }

            _arenaInfoTupleUpdatedBlockIndex = _agent.BlockIndex;

            try
            {
                var currentArenaInfo = await ApiClients.Instance.Arenaservicemanager.GetArenaInfoAsync(avatarAddress.ToString());
                var seasonResponse = GetSeasonResponseByBlockIndex(_arenaInfoTupleUpdatedBlockIndex);
                _currentSeasonId = seasonResponse.Id;
                return currentArenaInfo;
            }
            catch (Exception e)
            {
                NcDebug.LogError($"Failed to get current season: {e}");
                return null;
            }
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
                arenaInfo = await ApiClients.Instance.Arenaservicemanager.GetAvailableopponentsAsync(currentAvatarAddr.ToString());
            }
            catch (Exception e)
            {
                NcDebug.LogException(e);
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

            return arenaInfo;
        }
    }
}
