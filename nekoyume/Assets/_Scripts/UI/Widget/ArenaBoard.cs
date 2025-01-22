using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.Game.Controller;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Arena.Board;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using GeneratedApiNamespace.ArenaServiceClient;
    using Libplanet.Crypto;
    using Nekoyume.ApiClient;
    using Nekoyume.Blockchain;
    using Nekoyume.L10n;
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
        private ArenaBoardBillboard _billboard;

        [SerializeField]
        private ArenaBoardPlayerScroll _playerScroll;

        [SerializeField]
        private Button _backButton;

        private SeasonResponse _seasonData;
        private List<AvailableOpponentResponse> _boundedData;

        protected override void Awake()
        {
            base.Awake();

            InitializeScrolls();

            _backButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                Find<ArenaJoin>().Show();
                Close();
            }).AddTo(gameObject);
        }

        public async UniTaskVoid ShowAsync(bool ignoreShowAnimation = false)
        {
            var loading = Find<LoadingScreen>();
            loading.Show(LoadingScreen.LoadingType.Arena);
            var sw = new Stopwatch();
            sw.Start();
            var blockTipStateRootHash = Game.Game.instance.Agent.BlockTipStateRootHash;

            List<AvailableOpponentResponse> response = null;
            await ApiClients.Instance.Arenaservicemanager.Client.GetAvailableopponentsAsync(ArenaServiceManager.CreateCurrentJwt(),
                on200AvailableOpponents: (result) =>
                {
                    response = result?.ToList();
                },
                onError: (error) =>
                {
                    NcDebug.LogError($"[ArenaBoard] Failed to get available opponents | Error: {error}");
                    Find<OneButtonSystem>().Show(L10nManager.Localize("UI_ARENABOARD_GET_FAILED"),
                        L10nManager.Localize("UI_YES"), null);
                }
            );

            if (response == null)
            {
                return;
            }

            //시즌 시작 또는 인터벌시작 직후 최초 리스트가없는경우
            if (response.Count == 0)
            {
                await ApiClients.Instance.Arenaservicemanager.Client.PostAvailableopponentsRefreshAsync(ArenaServiceManager.CreateCurrentJwt(),
                    on200AvailableOpponents: (result) =>
                    {
                        response = result?.ToList();
                    },
                    onError: (error) =>
                    {
                        NcDebug.LogError($"[ArenaBoard] Failed to get first available opponents | Error: {error}");
                        Find<OneButtonSystem>().Show(L10nManager.Localize("UI_ARENABOARD_GET_FAILED"),
                            L10nManager.Localize("UI_YES"), null);
                    }
                );
            }

            loading.Close();
            if (response == null)
            {
                return;
            }

            // todo: 아레나서비스 리프레시중인경우일수있음 체크필요
            if (response.Count == 0)
            {
                NcDebug.LogError("No available opponents found for the arena. Please try again later.");
                return;
            }

            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            _seasonData = RxProps.GetSeasonResponseByBlockIndex(blockIndex);
            _boundedData = response;
            Find<HeaderMenuStatic>().Show(HeaderMenuStatic.AssetVisibleState.Arena);
            UpdateBillboard();
            UpdateScrolls();
            base.Show(ignoreShowAnimation);
            sw.Stop();
            NcDebug.Log($"[Arena] Loading Complete. {sw.Elapsed}");
        }

        private void UpdateBillboard()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                _billboard.SetData(
                    _so.SeasonText,
                    _so.Rank,
                    _so.WinCount,
                    _so.LoseCount,
                    _so.CP,
                    _so.Rating);
                return;
            }
#endif
            var player = RxProps.PlayerArenaInfo.Value;
            if (player is null)
            {
                NcDebug.Log($"{nameof(RxProps.PlayerArenaInfo)} is null");
                _billboard.SetData();
                return;
            }

            if (!RxProps.ArenaInfo.HasValue)
            {
                NcDebug.Log($"{nameof(RxProps.ArenaInfo)} is null");
                _billboard.SetData();
                return;
            }

            var win = 0;
            var lose = 0;
            var currentInfo = RxProps.ArenaInfo.Value;
            if (currentInfo is not null)
            {
                win = currentInfo.TotalWin;
                lose = currentInfo.TotalLose;
            }

            var cp = player.Cp;
            _billboard.SetData(
                "season",
                player.Rank,
                win,
                lose,
                cp,
                player.Score);
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
                    canFight = e.IsAttacked,
                    address = e.AvatarAddress,
                    guildName = e.ClanImageURL
                }).ToList();
            return (scrollData, 0);
        }

        private async UniTask RefreshArenaBoardAsync()
        {
            // todo : 로딩 연출 필요
            // 무료갱신이 아닌경우
            if (RxProps.ArenaInfo.Value.NextRefreshNCGCost > 0)
            {
                // todo : 아레나서비스
                // 재화량, 받는위치 수정해야함.
                var logId = await ActionManager.Instance.TransferAssetsForArenaBoardRefresh(States.Instance.AgentState.address,
                                        States.Instance.AgentState.address,
                                        new Libplanet.Types.Assets.FungibleAssetValue()); // 재화량을 0으로 설정

                if (logId == -1)
                {
                    NcDebug.LogError("[ArenaBoard] Refresh failed. Please try again later.");
                    return;
                }

                TicketPurchaseLogResponse refreshTicketResponse = null;
                // 서비스에서 tx확인 및 갱신완료될때까지 폴링
                int[] initialPollingIntervals = { 8000, 4000, 2000, 1000 }; // 초기 요청시간: 8s, 4s, 2s, 1s
                int maxAdditionalAttempts = 30; // 1초가된후 최대 요청개수

                async UniTask<bool> PerformPollingAsync()
                {
                    await ApiClients.Instance.Arenaservicemanager.Client.GetTicketsRefreshPurchaselogsAsync(logId, ArenaServiceManager.CreateCurrentJwt(),
                        on200PurchaseLogId: (result) =>
                        {
                            refreshTicketResponse = result;
                        },
                        onError: (error) =>
                        {
                            NcDebug.LogError($"[ArenaBoard] Error while polling for available opponents | Error: {error}");
                        }
                    );

                    return refreshTicketResponse != null && refreshTicketResponse.PurchaseStatus == PurchaseStatus.SUCCESS;
                }

                bool isPollingSuccessful = false; // 폴링 성공 여부를 저장할 변수
                // 초기 요청시간을 줄여가며 폴링 시작
                foreach (var interval in initialPollingIntervals)
                {
                    if (await PerformPollingAsync())
                    {
                        NcDebug.Log("[ArenaBoard] Refresh completed.");
                        isPollingSuccessful = true; // 폴링 성공 시 플래그 설정
                        break; // 성공 시 더 이상 요청하지 않도록 break
                    }
                    await UniTask.Delay(interval);
                }

                // 1초 간격으로 추가 폴링
                for (int i = 0; i < maxAdditionalAttempts && !isPollingSuccessful; i++) // 성공하지 않은 경우에만 추가 요청
                {
                    if (await PerformPollingAsync())
                    {
                        NcDebug.Log("[ArenaBoard] Refresh completed.");
                        isPollingSuccessful = true; // 폴링 성공 시 플래그 설정
                        break; // 성공 시 더 이상 요청하지 않도록 break
                    }
                    await UniTask.Delay(1000); // 1 second interval
                }

                if (refreshTicketResponse == null)
                {
                    NcDebug.LogError("[ArenaBoard] Response is null after refresh.");
                }
            }

            List<AvailableOpponentResponse> response = null;
            await ApiClients.Instance.Arenaservicemanager.Client.PostAvailableopponentsRefreshAsync(ArenaServiceManager.CreateCurrentJwt(),
                on200AvailableOpponents: (result) =>
                {
                    response = result?.ToList();
                },
                onError: (error) =>
                {
                    NcDebug.LogError($"[ArenaBoard] Failed to get free available opponents | Error: {error}");
                    Find<OneButtonSystem>().Show(L10nManager.Localize("UI_ARENABOARD_GET_FAILED"),
                        L10nManager.Localize("UI_YES"), null);
                }
            );

            if (response == null || response.Count == 0)
            {
                NcDebug.LogError("[ArenaBoard] Response is null after free refresh.");
                return;
            }
            _boundedData = response;
            UpdateScrolls();
        }

        public void RefreshArenaBoard()
        {
            RefreshArenaBoardAsync().Forget();
        }
    }
}
