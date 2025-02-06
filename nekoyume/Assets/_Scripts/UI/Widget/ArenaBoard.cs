using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.Game.Controller;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Arena.Board;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Nekoyume.UI
{
    using System.Globalization;
    using System.Numerics;
    using GeneratedApiNamespace.ArenaServiceClient;
    using Libplanet.Crypto;
    using Nekoyume.Action.Arena;
    using Nekoyume.ApiClient;
    using Nekoyume.Blockchain;
    using Nekoyume.Helper;
    using Nekoyume.L10n;
    using Nekoyume.Model.State;
    using TMPro;
    using UniRx;

    public class ArenaBoard : Widget
    {
#if UNITY_EDITOR
        [SerializeField]
        private bool _useSo;

        [SerializeField]
        private ArenaBoardSO _so;
#endif
        [SerializeField]
        private ArenaBoardPlayerScroll _playerScroll;

        [SerializeField]
        private Button _backButton;

        [SerializeField]
        private DetailedCharacterView _characterView;

        [SerializeField]
        private TextMeshProUGUI _myName;

        [SerializeField]
        private TextMeshProUGUI _myCp;

        [SerializeField]
        private TextMeshProUGUI _myRatingAndScore;

        [SerializeField]
        private TextMeshProUGUI _myScoreChangesInRound;

        [SerializeField]
        private TextMeshProUGUI _myWinLose;

        [SerializeField]
        private TextMeshProUGUI _myWinLoseChangesInRound;

        [SerializeField]
        private GameObject _clanObj;

        [SerializeField]
        private Image _myClanIcon;

        [SerializeField]
        private TextMeshProUGUI _myClanName;

        [SerializeField]
        private GameObject _loadingObj;

        [SerializeField]
        private ConditionalButton _refreshBtn;
        [SerializeField]
        private TextMeshProUGUI _refeshCountText;

        private SeasonResponse _seasonData;
        private List<AvailableOpponentResponse> _boundedData;

        protected override void Awake()
        {
            base.Awake();

            InitializeScrolls();
            _loadingObj.SetActive(false);

            _backButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();

                //캐싱한 시즌정보들에 시즌이없는경우 다음구간 시즌리스트를 가져와야해서 비동기 실행으로 변경.
                if (RxProps.GetSeasonResponseByBlockIndex(Game.Game.instance.Agent.BlockIndex) == null)
                {
                    Find<ArenaJoin>().ShowAsync().Forget();
                }
                else
                {
                    Find<ArenaJoin>().Show();
                }
                Close();
            }).AddTo(gameObject);

            _refreshBtn.OnClickSubject.Subscribe(_ =>
            {
                RefreshArenaBoard();
            }).AddTo(gameObject);
        }

        private void HandleArenaError(string errorl10nKey, string logMessage = null)
        {
            if (!string.IsNullOrEmpty(logMessage))
            {
                NcDebug.LogError(logMessage);
            }
            Find<LoadingScreen>().Close();
            AudioController.PlayClick();
            Close();
            Find<ArenaJoin>().Show();
            Find<IconAndButtonSystem>().Show(
                    "UI_ERROR",
                    errorl10nKey,
                    "UI_OK");
        }

        public async UniTaskVoid ShowAsync(bool ignoreShowAnimation = false)
        {
            var loading = Find<LoadingScreen>();
            loading.Show(LoadingScreen.LoadingType.Arena);
            var sw = new Stopwatch();
            sw.Start();
            var blockTipStateRootHash = Game.Game.instance.Agent.BlockTipStateRootHash;
            List<AvailableOpponentResponse> response = null;
            bool isFirst = false;

            //로딩이 아닌경우에만 리스트요청하도록
            if (!_loadingObj.activeSelf)
            {
                await ApiClients.Instance.Arenaservicemanager.Client.GetAvailableopponentsAsync(ArenaServiceManager.CreateCurrentJwt(),
                    on200: (result) =>
                    {
                        response = result?.ToList();
                    },
                    on404: (result) =>
                    {
                        //최초진입시 발생할수있어 로그수준을 낮춤.
                        NcDebug.Log("[ArenaBoard] Unable to retrieve available opponents. Error details: " + result);
                        isFirst = true;
                    },
                    onError: (error) =>
                    {
                        NcDebug.LogError($"[ArenaBoard] Failed to get available opponents | Error: {error}");
                    }
                );
                //시즌 시작 또는 인터벌시작 직후 최초 리스트가없는경우
                if (isFirst)
                {
                    try
                    {
                        await ApiClients.Instance.Arenaservicemanager.Client.PostAvailableopponentsRefreshAsync(ArenaServiceManager.CreateCurrentJwt(),
                            on200: (result) =>
                            {
                                response = result?.ToList();
                            },
                            on423: _ =>
                            {
                                throw new ArenaServiceException("UI_ARENA_SERVICE_LOCKED");
                            },
                            onError: (error) =>
                            {
                                NcDebug.LogError($"[ArenaBoard] Failed to get first available opponents | Error: {error}");
                            }
                        );
                    }
                    catch (Exception ex)
                    {
                        NcDebug.LogError($"[ArenaBoard] Exception occurred while refreshing available opponents | Error: {ex.Message}");
                        HandleArenaError(ex.Message);
                        return;
                    }
                }
                if (response == null)
                {
                    HandleArenaError("UI_ARENABOARD_GET_FAILED");
                    return;
                }

                // todo: 아레나서비스 리프레시중인경우일수있음 체크필요
                if (response.Count == 0)
                {
                    HandleArenaError("UI_ARENA_GET_OPPONENTS_FAILED", "No available opponents found for the arena. Please try again later.");
                    return;
                }
            }
            else
            {
                // nre방지를위해서 빈 list 생성
                response = new List<AvailableOpponentResponse>();
            }

            var arenaInfoResponse = await RxProps.ArenaInfo.UpdateAsync(blockTipStateRootHash);
            if (arenaInfoResponse == null)
            {
                HandleArenaError("UI_ARENA_INFO_GET_FAILED");
                return;
            }

            loading.Close();
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            _seasonData = RxProps.GetSeasonResponseByBlockIndex(blockIndex);
            if (_seasonData == null)
            {
                HandleArenaError("UI_ARENA_SEASONDATA_GET_FAILED");
                return;
            }
            _boundedData = response;
            Find<HeaderMenuStatic>().Show(HeaderMenuStatic.AssetVisibleState.Arena);
            UpdateBillboard();
            UpdateScrolls();

            // 리스트 갱신요청 풀링하는동안에는 버튼갱신하지않도록 예외처리
            if (!_loadingObj.activeSelf)
            {
                _refreshBtn.SetState(ConditionalButton.State.Normal);
                RefreshStateUpdate();
            }

            base.Show(ignoreShowAnimation);
            sw.Stop();
            NcDebug.Log($"[Arena] Loading Complete. {sw.Elapsed}");
        }

        private void UpdateBillboard()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                _characterView.SetByAvatarState(States.Instance.CurrentAvatarState);
                _myName.text = States.Instance.CurrentAvatarState.NameWithHash;
                _myCp.text = $"CP {_so.CP.ToString("N0", CultureInfo.CurrentCulture)}";
                _myRatingAndScore.text = $"{_so.Rank.ToString("N0", CultureInfo.CurrentCulture)} | {_so.Rating.ToString("N0", CultureInfo.CurrentCulture)}";
                _myScoreChangesInRound.text = "";
                _myWinLose.text = $"W {_so.WinCount.ToString("N0", CultureInfo.CurrentCulture)} | L {_so.LoseCount.ToString("N0", CultureInfo.CurrentCulture)}";
                _myWinLoseChangesInRound.text = "";
                _clanObj.SetActive(false);
                return;
            }
#endif
            if (!RxProps.ArenaInfo.HasValue)
            {
                NcDebug.LogError($"{nameof(RxProps.ArenaInfo)} is null");
                return;
            }

            var currentInfo = RxProps.ArenaInfo.Value;

            _characterView.SetByAvatarState(States.Instance.CurrentAvatarState);
            _myName.text = States.Instance.CurrentAvatarState.NameWithHash;
            _myCp.text = $"CP {currentInfo.User.Cp.ToString("N0", CultureInfo.CurrentCulture)}";
            _myRatingAndScore.text = $"{currentInfo.Rank.ToString("N0", CultureInfo.CurrentCulture)} | <color=#86D2FF>{currentInfo.Score.ToString("N0", CultureInfo.CurrentCulture)}";
            _myScoreChangesInRound.text = string.Format("{0:+#;-#;0}", currentInfo.CurrentRoundScoreChange);
            var currentRoundWin = currentInfo.TotalWin - currentInfo.CurrentRoundWinChange;
            var currentRoundLose = currentInfo.TotalLose - currentInfo.CurrentRoundLoseChange;
            _myWinLose.text = $"W {currentRoundWin.ToString("N0", CultureInfo.CurrentCulture)} <color=#FFFFFF>|</color> L {currentRoundLose.ToString("N0", CultureInfo.CurrentCulture)}";
            _myWinLoseChangesInRound.text = $"{currentInfo.CurrentRoundWinChange} / {currentInfo.CurrentRoundLoseChange}";
            _clanObj.SetActive(currentInfo.ClanInfo != null);
            if (currentInfo.ClanInfo != null)
            {
                Util.DownloadTexture(currentInfo.ClanInfo.ImageURL).ContinueWith((result) =>
                {
                    _myClanIcon.sprite = result;
                });
                _myClanName.text = currentInfo.ClanInfo.Name;
            }
        }

        private void InitializeScrolls()
        {
            _playerScroll.OnClickCharacterView.Subscribe(async index =>
                {
#if UNITY_EDITOR
                    if (_useSo && _so)
                    {
                        NotificationSystem.Push(
                            MailType.System,
                            "Cannot open when use mock data in editor mode",
                            NotificationCell.NotificationType.Alert);
                        return;
                    }
#endif
                    var avatarStates = await Game.Game.instance.Agent.GetAvatarStatesAsync(
                        new[] { new Address(_boundedData[index].AvatarAddress) });
                    var avatarState = avatarStates.Values.First();
                    Find<FriendInfoPopup>().ShowAsync(avatarState, BattleType.Arena).Forget();
                })
                .AddTo(gameObject);

            _playerScroll.OnClickChoice.Subscribe(index =>
                {
#if UNITY_EDITOR
                    if (_useSo && _so)
                    {
                        NotificationSystem.Push(
                            MailType.System,
                            "Cannot battle when use mock data in editor mode",
                            NotificationCell.NotificationType.Alert);
                        return;
                    }
#endif
                    var data = _boundedData[index];
                    if (ReactiveAvatarState.ActionPoint < Action.Arena.Battle.CostAp)
                    {
                        Find<HeaderMenuStatic>().ActionPoint.ShowMaterialNavigationPopup();
                        return;
                    }

                    if (RxProps.ArenaInfo.Value.BattleTicketStatus.RemainingTicketsPerRound == 0)
                    {
                        Find<ArenaTicketPopup>().Show();
                        return;
                    }
                    Close();
                    Find<ArenaBattlePreparation>().Show(
                        _seasonData,
                        _boundedData[index]);
                })
                .AddTo(gameObject);
        }

        private void UpdateScrolls()
        {
            var (scrollData, playerIndex) =
                GetScrollData();
            _playerScroll.SetData(scrollData, playerIndex);
        }

        private (List<ArenaBoardPlayerItemData> scrollData, int playerIndex)
            GetScrollData()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                return (_so.ArenaBoardPlayerScrollData, 0);
            }
#endif

            var currentAvatarAddr = States.Instance.CurrentAvatarState.address;
            var scrollData =
                _boundedData.Select(e => new ArenaBoardPlayerItemData
                {
                    name = e.NameWithHash,
                    level = e.Level,
                    fullCostumeOrArmorId = e.PortraitId,
                    titleId = null,
                    cp = (int)e.Cp,
                    score = e.Score,
                    rank = e.Rank,
                    expectWinDeltaScore = e.ScoreGainOnWin,
                    interactableChoiceButton = true,
                    canFight = !e.IsAttacked,
                    address = e.AvatarAddress,
                    guildName = e.ClanInfo?.Name,
                    guildImgUrl = e.ClanInfo?.ImageURL,
                    isVictory = e.IsVictory,
                    scoreOnLose = e.ScoreLossOnLose,
                    scoreOnWin = e.ScoreGainOnWin
                }).ToList();
            return (scrollData, 0);
        }

        private void RefreshStateUpdate()
        {
            var currentRefreshCount = RxProps.ArenaInfo.Value.RefreshTicketStatus.RemainingTicketsPerRound + RxProps.ArenaInfo.Value.RefreshTicketStatus.RemainingPurchasableTicketsPerRound;
            var maxRefreshCount = _seasonData.RefreshTicketPolicy.DefaultTicketsPerRound + _seasonData.RefreshTicketPolicy.MaxPurchasableTicketsPerRound;
            //최초라운드 시작시 자동으로 목록갱신해주는것때문에 실제 횟수에서 -1로 표기한다.
            _refeshCountText.text = L10nManager.Localize("UI_ARENA_REFRESH_COUNT", currentRefreshCount, maxRefreshCount - 1);

            if (RxProps.ArenaInfo.Value.RefreshTicketStatus.RemainingTicketsPerRound == 0)
            {
                // 더이상 구매할수없는경우
                if (RxProps.ArenaInfo.Value.RefreshTicketStatus.RemainingPurchasableTicketsPerRound == 0)
                {
                    _refreshBtn.SetState(ConditionalButton.State.Disabled);
                    _refreshBtn.SetText(L10nManager.Localize("UI_ARENA_REFRESH_BTN"));
                }
                else
                {
                    var nextCosts = RxProps.ArenaInfo.Value.RefreshTicketStatus.NextNCGCosts;
                    _refreshBtn.SetText(L10nManager.Localize("UI_ARENA_REFRESH_BTN_WITH_NCG", nextCosts.FirstOrDefault()));
                }
            }
            else
            {
                _refreshBtn.SetText(L10nManager.Localize("UI_ARENA_REFRESH_BTN"));
            }
        }

        private async UniTask RefreshArenaBoardAsync()
        {
            // 무료갱신이 아닌경우
            _refreshBtn.SetState(ConditionalButton.State.Disabled);
            _loadingObj.SetActive(true);

            if (RxProps.ArenaInfo.Value.RefreshTicketStatus.RemainingTicketsPerRound == 0)
            {
                var nextCost = RxProps.ArenaInfo.Value.RefreshTicketStatus.NextNCGCosts.FirstOrDefault();
                var goldCurrency = States.Instance.GoldBalanceState.Gold.Currency;
                var cost = Libplanet.Types.Assets.FungibleAssetValue.Parse(goldCurrency, nextCost.ToString());

                int logId = -1;
                try
                {
                    logId = await ActionManager.Instance.TransferAssetsForArenaBoardRefresh(States.Instance.AgentState.address,
                                           new Address(RxProps.OperationAccountAddress),
                                           cost);
                }
                catch (Exception ex)
                {
                    NcDebug.LogError("[ArenaBoard] Refresh failed. Please try again later.");
                    _loadingObj.SetActive(false);
                    _refreshBtn.SetState(ConditionalButton.State.Normal);
                    Find<IconAndButtonSystem>().Show(
                        "UI_ERROR",
                        "UI_ARENABOARD_GET_LOCKED",
                        "UI_OK");
                    return;
                }

                if (logId == -1)
                {
                    NcDebug.LogError("[ArenaBoard] Refresh failed. Please try again later.");
                    _loadingObj.SetActive(false);
                    _refreshBtn.SetState(ConditionalButton.State.Normal);
                    Find<IconAndButtonSystem>().Show(
                        "UI_ERROR",
                        "UI_ARENABOARD_GET_FAILED",
                        "UI_OK");
                    return;
                }

                TicketPurchaseLogResponse refreshTicketResponse = null;
                // 서비스에서 tx확인 및 갱신완료될때까지 폴링
                int[] initialPollingIntervals = { 8000, 4000, 2000, 1000 }; // 초기 요청시간: 8s, 4s, 2s, 1s
                int maxAdditionalAttempts = 30; // 1초가된후 최대 요청개수

                async UniTask<PurchaseStatus> PerformPollingAsync()
                {
                    await ApiClients.Instance.Arenaservicemanager.Client.GetTicketsRefreshPurchaselogsAsync(logId, ArenaServiceManager.CreateCurrentJwt(),
                        on200: (result) =>
                        {
                            refreshTicketResponse = result;
                        },
                        onError: (error) =>
                        {
                            NcDebug.LogError($"[ArenaBoard] Error while polling for available opponents | Error: {error}");
                        }
                    );

                    return refreshTicketResponse.PurchaseStatus;
                }

                PurchaseStatus pollingResult = PurchaseStatus.PENDING;
                bool isPollingSuccessful = false; // 폴링 성공 여부를 저장할 변수
                // 초기 요청시간을 줄여가며 폴링 시작
                foreach (var interval in initialPollingIntervals)
                {
                    pollingResult = await PerformPollingAsync();
                    if (pollingResult == PurchaseStatus.SUCCESS)
                    {
                        NcDebug.Log("[ArenaBoard] Refresh completed.");
                        isPollingSuccessful = true; // 폴링 성공 시 플래그 설정
                        break; // 성공 시 더 이상 요청하지 않도록 break
                    }

                    if (pollingResult == PurchaseStatus.NOT_FOUND_TRANSFER_ASSETS_ACTION ||
                        pollingResult == PurchaseStatus.INSUFFICIENT_PAYMENT ||
                        pollingResult == PurchaseStatus.INVALID_RECIPIENT ||
                        pollingResult == PurchaseStatus.DUPLICATE_TRANSACTION ||
                        pollingResult == PurchaseStatus.TX_FAILED)
                    {
                        NcDebug.LogError("[ArenaBoard] Refresh failed due to an error in polling.");
                        isPollingSuccessful = true; // 실패케이스로 폴링 멈추도록 플래그 설정
                        break;
                    }
                    await UniTask.Delay(interval);
                }

                // 1초 간격으로 추가 폴링
                for (int i = 0; i < maxAdditionalAttempts && !isPollingSuccessful; i++) // 성공하지 않은 경우에만 추가 요청
                {
                    pollingResult = await PerformPollingAsync();
                    if (pollingResult == PurchaseStatus.SUCCESS)
                    {
                        NcDebug.Log("[ArenaBoard] Refresh completed.");
                        isPollingSuccessful = true; // 폴링 성공 시 플래그 설정
                        break; // 성공 시 더 이상 요청하지 않도록 break
                    }
                    if (pollingResult == PurchaseStatus.NOT_FOUND_TRANSFER_ASSETS_ACTION ||
                        pollingResult == PurchaseStatus.INSUFFICIENT_PAYMENT ||
                        pollingResult == PurchaseStatus.INVALID_RECIPIENT ||
                        pollingResult == PurchaseStatus.DUPLICATE_TRANSACTION ||
                        pollingResult == PurchaseStatus.TX_FAILED)
                    {
                        NcDebug.LogError("[ArenaBoard] Refresh failed due to an error in polling.");
                        isPollingSuccessful = true; // 실패케이스로 폴링 멈추도록 플래그 설정
                        break;
                    }
                    await UniTask.Delay(1000); // 1 second interval
                }

                if (refreshTicketResponse == null)
                {
                    NcDebug.LogError("[ArenaBoard] Response is null after refresh.");
                }

                if (pollingResult != PurchaseStatus.SUCCESS)
                {
                    NcDebug.LogWarning("[ArenaBoard] Refresh failed. Polling result: " + pollingResult);
                    _loadingObj.SetActive(false);
                    _refreshBtn.SetState(ConditionalButton.State.Normal);
                    Find<IconAndButtonSystem>().Show(
                        "UI_ERROR",
                        "UI_ARENABOARD_GET_FAILED_" + pollingResult.ToString(),
                        "UI_OK");
                    return;
                }
            }

            List<AvailableOpponentResponse> response = null;
            try
            {
                await ApiClients.Instance.Arenaservicemanager.Client.PostAvailableopponentsRefreshAsync(ArenaServiceManager.CreateCurrentJwt(),
                    on200: (result) =>
                    {
                        response = result?.ToList();
                    },
                    on423: _ =>
                    {
                        throw new ArenaServiceException("UI_ARENA_SERVICE_LOCKED");
                    }
                    ,
                    onError: (error) =>
                    {
                        NcDebug.LogError($"[ArenaBoard] Failed to get available opponents | Error: {error}");
                    }
                );

            }
            catch (Exception e)
            {
                NcDebug.LogError("[ArenaBoard] Response is null after refresh.");
                _loadingObj.SetActive(false);
                _refreshBtn.SetState(ConditionalButton.State.Normal);
                Find<IconAndButtonSystem>().Show(
                    "UI_ERROR",
                    e.Message,
                    "UI_OK");
                return;
            }

            if (response == null || response.Count == 0)
            {
                NcDebug.LogError("[ArenaBoard] Response is null after refresh.");
                _loadingObj.SetActive(false);
                _refreshBtn.SetState(ConditionalButton.State.Normal);
                Find<IconAndButtonSystem>().Show(
                    "UI_ERROR",
                    "UI_ARENABOARD_GET_FAILED",
                    "UI_OK");
                return;
            }
            _boundedData = response;
            UpdateScrolls();

            var blockTipStateRootHash = Game.Game.instance.Agent.BlockTipStateRootHash;
            await RxProps.ArenaInfo.UpdateAsync(blockTipStateRootHash);

            _loadingObj.SetActive(false);
            _refreshBtn.SetState(ConditionalButton.State.Normal);
            RefreshStateUpdate();
        }

        public void RefreshArenaBoard()
        {
            if (_loadingObj.activeSelf)
            {
                NcDebug.LogWarning("[ArenaBoard] Loading is in progress, cannot refresh the arena board.");
                return;
            }

            if (RxProps.ArenaInfo.Value.RefreshTicketStatus.RemainingTicketsPerRound == 0)
            {
                //더 이상 갱신못하는 경우
                if (RxProps.ArenaInfo.Value.RefreshTicketStatus.RemainingPurchasableTicketsPerRound == 0)
                {
                    Find<OneButtonSystem>().Show(L10nManager.Localize("UI_ARENA_BOARD_REFRESH_MAX_LIMIT"),
                        L10nManager.Localize("UI_YES"), () =>
                        {

                        });
                }
                //재화 소모로 갱신가능한경우
                else
                {
                    var nextCost = RxProps.ArenaInfo.Value.RefreshTicketStatus.NextNCGCosts.FirstOrDefault();
                    var goldCurrency = States.Instance.GoldBalanceState.Gold.Currency;
                    var cost = Libplanet.Types.Assets.FungibleAssetValue.Parse(goldCurrency, nextCost.ToString());
                    Find<PaymentPopup>().ShowCheckPaymentNCG(
                        States.Instance.GoldBalanceState.Gold,
                        cost,
                        L10nManager.Localize("UI_ARENA_BOARD_REFRESH_NCG_REQUIRE", nextCost.ToString()),
                        () =>
                        {
                            RefreshArenaBoardAsync().Forget();
                        }
                    );
                }
            }
            //무료로 갱신가능한경우
            else
            {
                Find<TwoButtonSystem>().Show(L10nManager.Localize("UI_ARENA_BOARD_FREE_REFRESH_NOTICE"),
                    L10nManager.Localize("UI_YES"),
                    L10nManager.Localize("UI_NO"),
                    () =>
                    {
                        RefreshArenaBoardAsync().Forget();
                    });
            }
        }

        public void OnClickClanRankingBtn()
        {
            Find<ClanRankPopup>().Show();
        }
    }
}
