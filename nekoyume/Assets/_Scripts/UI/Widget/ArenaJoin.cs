using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cysharp.Threading.Tasks;
using Lib9c.Renderers;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Arena.Join;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using GeneratedApiNamespace.ArenaServiceClient;
    using Nekoyume.ApiClient;
    using UniRx;

    public class ArenaJoin : Widget
    {
        private static int _barScrollCellCount;
        private static int BarScrollIndexOffset => (int)math.ceil(_barScrollCellCount / 2f) - 1;

#if UNITY_EDITOR
        [SerializeField]
        private bool _useSo;

        [SerializeField]
        private ArenaJoinSO _so;
#endif

        [SerializeField]
        private ArenaJoinSeasonScroll _scroll;

        [SerializeField]
        private ArenaJoinSeasonBarScroll _barScroll;

        [SerializeField]
        private ArenaJoinSeasonInfo _info;

        [SerializeField]
        private TextMeshProUGUI _bottomButtonText;

        [SerializeField]
        private ConditionalButton _joinButton;

        [Obsolete("추후에 삭제될 예정.")]
        [SerializeField]
        private ConditionalCostButton _paymentButton;

        [SerializeField]
        private ArenaJoinMissionButton _missionButton;

        [SerializeField]
        private Button _backButton;

        [SerializeField]
        private GameObject baseArenaJoinObject;

        private readonly List<IDisposable> _disposablesForShow = new();
        private int _totalMedalCountForThisChampionship = 0;

        protected override void Awake()
        {
            base.Awake();

            InitializeScrolls();
            InitializeBottomButtons();

            _backButton.OnClickAsObservable().Subscribe(_ =>
            {
                Close(true);
                Lobby.Enter(true);
            }).AddTo(gameObject);

            CloseWidget = () =>
            {
                Close(true);
                Lobby.Enter(true);
            };
        }

        public async UniTaskVoid ShowAsync(
            bool ignoreShowAnimation = false)
        {
            var sw = new Stopwatch();
            sw.Start();
            var loading = Find<LoadingScreen>();
            loading.Show(LoadingScreen.LoadingType.Arena);
            var blockTipStateRootHash = Game.Game.instance.Agent.BlockTipStateRootHash;
            var arenaInfoResponse = await RxProps.ArenaInfo.UpdateAsync(blockTipStateRootHash);
            if (arenaInfoResponse == null)
            {
                loading.Close();
                Lobby.Enter(true);
                Find<IconAndButtonSystem>().Show(
                        "UI_ERROR",
                        "UI_ARENAJOIN_INFO_GET_FAILED",
                        "UI_OK");
                return;
            }

            await RxProps.UpdateSeasonResponsesAsync(Game.Game.instance.Agent.BlockIndex);
            if (RxProps.ArenaSeasonResponses.Value.Count != 0 && 
                RxProps.ArenaSeasonResponses.Value.Last().EndBlockIndex > Game.Game.instance.Agent.BlockIndex)
            {
                loading.Close();
                Lobby.Enter(true);
                Find<IconAndButtonSystem>().Show(
                        "UI_ERROR",
                        "UI_ARENAJOIN_SEASON_INFO_GET_FAILED",
                        "UI_OK");
                return;
            }

            await ApiClients.Instance.Arenaservicemanager.Client.GetUsersClassifybychampionshipMedalsAsync(Game.Game.instance.Agent.BlockIndex,
                on200OK: (result) =>
                {
                    _totalMedalCountForThisChampionship = result.TotalMedalCountForThisChampionship;
                },
                onError: (error) =>
                {
                    NcDebug.LogError(error);
                });

            loading.Close();
            sw.Stop();
            NcDebug.Log($"[Arena] Loading Complete. {sw.Elapsed}");
            Show(ignoreShowAnimation);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Arena);
            AudioController.instance.PlayMusic(AudioController.MusicCode.Ranking);
            UpdateScrolls();
            UpdateInfo();

            RxProps.ArenaInfo
                .Subscribe(tuple => UpdateBottomButtons())
                .AddTo(_disposablesForShow);
            baseArenaJoinObject.SetActive(true);

            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesForShow.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        public void OnRenderJoinArena(ActionEvaluation<JoinArena> eval)
        {
            // 메모: 이 함수는 더 이상 사용되지 않으므로 삭제 예정입니다.
        }

        /// <summary>
        /// Used from Awake() function once.
        /// </summary>
        private void InitializeScrolls()
        {
            _scroll.OnSelectionChanged
                .Select(ReverseScrollIndex)
                .Subscribe(reversedIndex =>
                {
                    _barScroll.SelectCell(reversedIndex, false);
                    UpdateInfo();
                    UpdateBottomButtons();
                })
                .AddTo(gameObject);
            _barScroll.OnSelectionChanged
                .Select(ReverseScrollIndex)
                .Subscribe(reversedIndex =>
                {
                    _scroll.SelectCell(reversedIndex, false);
                    UpdateInfo();
                    UpdateBottomButtons();
                })
                .AddTo(gameObject);
        }

        private void UpdateScrolls()
        {
            var scrollData = GetScrollData();
            int selectedIndex = 0;

            if (scrollData is not null)
            {
                selectedIndex = scrollData?.ToList().FindIndex(item =>
                    item.SeasonData.Id == RxProps.CurrentArenaSeasonId) ?? 0;
            }

            _scroll.SetData(scrollData, selectedIndex);
            _barScrollCellCount = scrollData.Count;
            _barScroll.SetData(
                GetBarScrollData(_barScrollCellCount, BarScrollIndexOffset),
                ReverseScrollIndex(selectedIndex));
        }

        private IList<ArenaJoinSeasonItemData> GetScrollData()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                var championshipSeasonIds = _so.ArenaDataList
                    .Where(e => e.RoundDataBridge.ArenaType == Nekoyume.Model.EnumType.ArenaType.Season)
                    .Select(e => e.RoundDataBridge.Round)
                    .ToList();
                var arenaDataList = _so.ArenaDataList
                    .Select(data => new SeasonResponse())
                    .ToList();
                return arenaDataList.Select(data => new ArenaJoinSeasonItemData
                {
                    SeasonData = data,
                    SeasonNumber = data.Id,
                    ChampionshipSeasonNumbers = championshipSeasonIds
                }).ToList();
            }
#endif
            {
                return RxProps.ArenaSeasonResponses.Value
                    .Select(seasonResponse => new ArenaJoinSeasonItemData
                    {
                        SeasonData = seasonResponse,
                        SeasonNumber = seasonResponse.Id,
                        ChampionshipSeasonNumbers =
                            RxProps.GetSeasonNumbersOfChampionship()
                    }).ToList();
            }
        }

        private static IList<ArenaJoinSeasonBarItemData> GetBarScrollData(
            int barScrollCellCount, int barIndexOffset)
        {
            return Enumerable.Range(0, barScrollCellCount)
                .Select(index => new ArenaJoinSeasonBarItemData
                {
                    visible = index == barIndexOffset
                })
                .ToList();
        }

        private static int ReverseScrollIndex(int scrollIndex)
        {
            return _barScrollCellCount - scrollIndex - 1;
        }

        private void UpdateInfo()
        {
            var selectedRoundData = _scroll.SelectedItemData.SeasonData;
            _info.SetData(
                _scroll.SelectedItemData.GetRoundName(),
                selectedRoundData,
                GetRewardType(_scroll.SelectedItemData),
                // 메달 사라질것이므로 일단 널세팅
                null);

            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var season = RxProps.GetSeasonResponseByBlockIndex(blockIndex);
            var championshipRound = RxProps.ArenaSeasonResponses.Value.LastOrDefault().Id;
            if (season.Id > championshipRound)
            {
                _missionButton.Hide();
            }
            else
            {
                _missionButton.Show(GetConditions());
            }
        }

        /// <summary>
        /// Used from Awake() function once.
        /// </summary>
        private void InitializeBottomButtons()
        {
            void OnClickJoinButton()
            {
                AudioController.PlayClick();
                Find<ArenaBoard>().ShowAsync().Forget();
            }

            _joinButton.SetState(ConditionalButton.State.Normal);
            _joinButton.OnClickSubject.Subscribe(_ => OnClickJoinButton()).AddTo(gameObject);

            _info.OnSeasonBeginning
                .Merge(_info.OnSeasonEnded)
                .Subscribe(_ =>
                {
                    RxProps.UpdateArenaInfoToNext();
                    UpdateBottomButtons();
                })
                .AddTo(gameObject);
        }

        private void UpdateBottomButtons()
        {
            var selectedSeasonData = _scroll.SelectedItemData.SeasonData;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var isOpened = selectedSeasonData.StartBlockIndex <= blockIndex && blockIndex <= selectedSeasonData.EndBlockIndex;
            var arenaType = selectedSeasonData.ArenaType;
            UpdateJoinAndPaymentButton(arenaType, isOpened);
        }

        private void UpdateJoinAndPaymentButton(
            ArenaType arenaType,
            bool isOpened)
        {
            switch (arenaType)
            {
                case ArenaType.OFF_SEASON:
                    {
                        _bottomButtonText.enabled = false;
                        _joinButton.Interactable = isOpened;
                        _joinButton.gameObject.SetActive(true);
                        return;
                    }
                case ArenaType.SEASON:
                case ArenaType.CHAMPIONSHIP:
                    {
                        if (isOpened)
                        {
                            if (RxProps.ArenaInfo.Value is null)
                            {
                                _joinButton.gameObject.SetActive(false);
                                if (arenaType == ArenaType.CHAMPIONSHIP &&
                                    !CheckChampionshipConditions())
                                {
                                    _bottomButtonText.text =
                                        L10nManager.Localize("UI_NOT_ENOUGH_ARENA_MEDALS");
                                    _bottomButtonText.enabled = true;
                                    return;
                                }

                                _bottomButtonText.enabled = false;
                                return;
                            }

                            _bottomButtonText.enabled = false;
                            _joinButton.Interactable = true;
                            _joinButton.gameObject.SetActive(true);
                        }
                        else
                        {
                            if (arenaType == ArenaType.CHAMPIONSHIP &&
                                !CheckChampionshipConditions())
                            {
                                _bottomButtonText.text =
                                    L10nManager.Localize("UI_NOT_ENOUGH_ARENA_MEDALS");
                                _bottomButtonText.enabled = true;
                                _joinButton.gameObject.SetActive(false);
                                return;
                            }

                            _bottomButtonText.enabled = false;
                            _joinButton.Interactable = false;
                            _joinButton.gameObject.SetActive(true);
                        }

                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool CheckChampionshipConditions()
        {
            var selectedRoundData = _scroll.SelectedItemData.SeasonData;
            var medalTotalCount = _totalMedalCountForThisChampionship;
            var completeCondition = medalTotalCount >=
                selectedRoundData.RequiredMedalCount;
            return completeCondition;
        }

        private (int required, int current) GetConditions()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                return _so.Conditions;
            }
#endif

            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var lastChampionship = RxProps.ArenaSeasonResponses.Value.LastOrDefault();
            var avatarState = States.Instance.CurrentAvatarState;
            var requiredMedal = lastChampionship?.RequiredMedalCount ?? 0;

            // todo : 아레나서비스
            // 사용자 메달 숫자로 갱신시켜야함.
            var medalTotalCount = 0;
            return ((int)requiredMedal, medalTotalCount);
        }

        private ArenaJoinSeasonInfo.RewardType GetRewardType(ArenaJoinSeasonItemData data)
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                var soData = _so.ArenaDataList.FirstOrDefault(soData =>
                    soData.RoundDataBridge.ChampionshipId == data.SeasonData.Id &&
                    soData.RoundDataBridge.Round == data.SeasonData.Id);
                return soData is null
                    ? ArenaJoinSeasonInfo.RewardType.None
                    : soData.RewardType;
            }
#endif

            return data.SeasonData.ArenaType switch
            {
                ArenaType.OFF_SEASON => ArenaJoinSeasonInfo.RewardType.None,
                ArenaType.SEASON =>
                    ArenaJoinSeasonInfo.RewardType.Courage |
                    ArenaJoinSeasonInfo.RewardType.Medal |
                    ArenaJoinSeasonInfo.RewardType.NCG,
                ArenaType.CHAMPIONSHIP =>
                    ArenaJoinSeasonInfo.RewardType.Courage |
                    ArenaJoinSeasonInfo.RewardType.Medal |
                    ArenaJoinSeasonInfo.RewardType.NCG,
                // NOTE: Enable costume when championship rewards contains one.
                // ArenaJoinSeasonInfo.RewardType.Costume,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public void TutorialActionSeasonPassGuidePopup()
        {
            Find<SeasonPassNewPopup>().Show();
        }
    }
}
