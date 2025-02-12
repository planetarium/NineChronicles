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
using Nekoyume.Game;
using System.Reactive.Linq;

namespace Nekoyume.State
{
    using Libplanet.Common;
    using Nekoyume.L10n;
    using Nekoyume.UI;
    using System.Security.Cryptography;
    using UniRx;

    public static partial class RxProps
    {
        #region RxPropInternal
        // TODO!!!! Remove [`_arenaInfoTuple`] and use [`_playersArenaParticipant`] instead.
        private static readonly
            AsyncUpdatableRxProp<ArenaInfoResponse>
            _arenaInfo = new(UpdateArenaInfoAsync);

        private static readonly ReactiveProperty<int> _purchasedDuringInterval = new();
        private static readonly ReactiveProperty<long> _lastArenaBattleBlockIndex = new();
        private static readonly ReactiveProperty<ArenaTicketProgress>
            _arenaTicketsProgress = new(new ArenaTicketProgress());

        private static readonly ReactiveProperty<List<SeasonResponse>> _arenaSeasonResponses = new(new List<SeasonResponse>());
        #endregion RxPropInternal

        #region RxPropObservable
        public static IReadOnlyReactiveProperty<int> PurchasedDuringInterval => _purchasedDuringInterval;
        public static IReadOnlyReactiveProperty<long> LastArenaBattleBlockIndex => _lastArenaBattleBlockIndex;
        public static IReadOnlyReactiveProperty<ArenaTicketProgress>
            ArenaTicketsProgress => _arenaTicketsProgress;
        public static IReadOnlyAsyncUpdatableRxProp<ArenaInfoResponse>
            ArenaInfo => _arenaInfo;

        public static IReadOnlyReactiveProperty<List<SeasonResponse>> ArenaSeasonResponses => _arenaSeasonResponses;
        #endregion RxPropObservable

        private static int _currentSeasonId = -1;
        private static int _lastBattleLogId;

        private static int _lastRoundIndex = -1;

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
                    .Select(seasonResponse => seasonResponse.SeasonGroupId)
                    .ToList();
        }

        public static SeasonResponse GetSeasonResponseByBlockIndex(long blockIndex)
        {
            return ArenaSeasonResponses.Value.Where(seasonResponse => seasonResponse.StartBlockIndex <= blockIndex && blockIndex <= seasonResponse.EndBlockIndex).FirstOrDefault();
        }

        public static SeasonResponse GetNextSeasonResponseByBlockIndex(long blockIndex)
        {
            return ArenaSeasonResponses.Value
                .Where(seasonResponse => seasonResponse.StartBlockIndex >= blockIndex)
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

            //이미 Classifybychampionship된 응답을 한번 받은경우.
            if (_arenaSeasonResponses.Value.Count != 0)
            {
                var currentSeason = _arenaSeasonResponses.Value.Find(item =>
                    item.StartBlockIndex <= blockIndex && item.EndBlockIndex >= blockIndex
                );
                // 받은 응답에서 현재 시즌에 해당하는것이 없을경우 예외처리.
                if (currentSeason == null)
                {
                    _currentSeasonId = -1;
                    _isUpdatingSeasonResponses = false;
                    NcDebug.LogError("current season is null");
                    return;
                }
                _currentSeasonId = currentSeason.Id;
                _lastRoundIndex = currentSeason.GetCurrentRound(blockIndex)?.Id ?? -1;

                //이미 받은응답의 마지막 블록인덱스가 현재인댁스보다 큰경우 Classifybychampionship이 바뀌지않은것임으로 중복요청 방지.
                if (_arenaSeasonResponses.Value.Last().EndBlockIndex > blockIndex)
                    return;
            }

            _isUpdatingSeasonResponses = true;

            await ApiClients.Instance.Arenaservicemanager.Client.GetSeasonsClassifybychampionshipAsync(blockIndex,
                on200: response =>
                {
                    _arenaSeasonResponses.SetValueAndForceNotify(response.Seasons.ToList());

                    var currentSeason = _arenaSeasonResponses.Value.Find(item =>
                        item.StartBlockIndex <= blockIndex && item.EndBlockIndex >= blockIndex
                    );

                    if (currentSeason == null)
                    {
                        _currentSeasonId = -1;
                        _isUpdatingSeasonResponses = false;
                        NcDebug.LogError("current season is null");
                        return;
                    }

                    _currentSeasonId = currentSeason.Id;
                    _lastRoundIndex = currentSeason.GetCurrentRound(blockIndex)?.Id ?? -1;

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
            if (currentAvatar == null)
            {
                return null;
            }
            var currentAvatarAddr = currentAvatar.address;
            var portraitId = Util.GetPortraitId(BattleType.Arena);
            var cp = Util.TotalCP(BattleType.Arena);
            return await ApiClients.Instance.Arenaservicemanager.PostUsersAsync(currentAvatarAddr.ToString(), currentAvatar.NameWithHash, portraitId, cp, currentAvatar.level);
        }

        private static bool _hasSubscribedArenaTicketProgress = false;
        private static async UniTask InitializeArena()
        {
            await UpdateSeasonResponsesAsync(Game.Game.instance.Agent.BlockIndex);

            // 아바타 최초 아레나등록
            await PostUserAsync();

            // 로비화면에서 티켓정보를 보여주기 위해 인포 초기화
            await ArenaInfo.UpdateAsync(_agent.BlockTipStateRootHash);

            // 중복 구독을 방지
            if (!_hasSubscribedArenaTicketProgress)
            {
                Game.Game.instance.Agent.BlockIndexSubject
                    .StartWith(Game.Game.instance.Agent.BlockIndex)
                    .Subscribe(UpdateArenaTicketProgress)
                    .AddTo(_disposables);

                ArenaInfo.Subscribe((info) => UpdateArenaTicketProgress(Game.Game.instance.Agent.BlockIndex))
                    .AddTo(_disposables);

                _hasSubscribedArenaTicketProgress = true;
            }
        }

        public static void UpdateArenaTicketProgress(long blockIndex)
        {
            var currentSeason = GetSeasonResponseByBlockIndex(blockIndex);
            if (currentSeason == null)
            {
                NcDebug.LogError("Unable to retrieve current season information. Block index: " + blockIndex);
                _arenaTicketsProgress.Value.Reset(
                    5,
                    5);
                _arenaTicketsProgress.SetValueAndForceNotify(_arenaTicketsProgress.Value);
                return;
            }
            int maxTicketCount = currentSeason.BattleTicketPolicy.DefaultTicketsPerRound;
            var ticketResetInterval = currentSeason.RoundInterval;
            var progressedBlockRange =
                (blockIndex - currentSeason.StartBlockIndex) % ticketResetInterval;
            var currentArenaInfo = _arenaInfo.HasValue
                ? _arenaInfo.Value
                : null;
            if (currentArenaInfo is null)
            {
                _arenaTicketsProgress.Value.Reset(
                    maxTicketCount,
                    maxTicketCount,
                    (int)progressedBlockRange,
                    ticketResetInterval);
                _arenaTicketsProgress.SetValueAndForceNotify(_arenaTicketsProgress.Value);
                return;
            }
            var currentTicketCount = currentArenaInfo.BattleTicketStatus.RemainingTicketsPerRound;
            _arenaTicketsProgress.Value.Reset(
                currentTicketCount,
                maxTicketCount,
                (int)progressedBlockRange,
                ticketResetInterval,
                currentArenaInfo.BattleTicketStatus.TicketsPurchasedPerRound);
            _arenaTicketsProgress.SetValueAndForceNotify(
                _arenaTicketsProgress.Value);

            var currentBlockIndex = currentSeason.GetCurrentRound(blockIndex);
            if (currentBlockIndex?.Id != _lastRoundIndex)
            {
                _lastRoundIndex = currentBlockIndex?.Id ?? _lastRoundIndex;
                //라운드 바뀌는순간.
                if (Widget.Find<ArenaBoard>().IsActive() ||
                    Widget.Find<ArenaBattlePreparation>().IsActive())
                {
                    Lobby.Enter(false);
                    Game.Game.instance.Lobby.OnLobbyEnterEnd
                        .First()
                        .Subscribe(_ =>
                        {
                            Widget.Find<LobbyMenu>().Close();
                            Widget.Find<ArenaJoin>().ShowAsync().Forget();
                            Widget.Find<OneButtonSystem>().Show(L10nManager.Localize("UI_ARENA_ROUND_CHAGED"), L10nManager.Localize("UI_OK"), null);
                        });
                    MainCanvas.instance.InitWidgetInMain();
                }
            }
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

            try
            {
                var currentArenaInfo = await ApiClients.Instance.Arenaservicemanager.GetArenaInfoAsync(avatarAddress.ToString());
                return currentArenaInfo;
            }
            catch (Exception e)
            {
                NcDebug.LogError($"Failed to get current season: {e}");
                return null;
            }
        }
    }
}
