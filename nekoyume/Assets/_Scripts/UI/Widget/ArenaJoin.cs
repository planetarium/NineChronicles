using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cysharp.Threading.Tasks;
using Lib9c.Renderers;
using Nekoyume.Action;
using Nekoyume.Arena;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Arena.Join;
using Nekoyume.UI.Scroller;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Nekoyume.UI
{
    using Nekoyume.Helper;
    using UniRx;

    public class ArenaJoin : Widget
    {
        private enum InnerState
        {
            Idle,
            EarlyRegistration,
            RegistrationAndTransitionToArenaBoard,
        }

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

        [SerializeField]
        private ConditionalCostButton _paymentButton;

        [SerializeField]
        private ArenaJoinEarlyRegisterButton _earlyPaymentButton;

        [SerializeField]
        private ArenaJoinMissionButton _missionButton;

        [SerializeField]
        private Button _backButton;

        [SerializeField]
        private GameObject baseArenaJoinObject;

        private InnerState _innerState = InnerState.Idle;
        private readonly List<IDisposable> _disposablesForShow = new List<IDisposable>();

        protected override void Awake()
        {
            base.Awake();

            InitializeScrolls();
            InitializeBottomButtons();

            _backButton.OnClickAsObservable().Subscribe(_ =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            }).AddTo(gameObject);

            CloseWidget = () =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            };
        }

        public async UniTaskVoid ShowAsync(
            bool ignoreShowAnimation = false)
        {
            var sw = new Stopwatch();
            sw.Start();
            var loading = Find<LoadingScreen>();
            loading.Show(LoadingScreen.LoadingType.Arena);
            await UniTask.WhenAll(
                RxProps.ArenaInfoTuple.UpdateAsync(Game.Game.instance.Agent.BlockTipStateRootHash),
                RxProps.ArenaInformationOrderedWithScore.UpdateAsync(Game.Game.instance.Agent.BlockTipStateRootHash));
            loading.Close();
            sw.Stop();
            NcDebug.Log($"[Arena] Loading Complete. {sw.Elapsed}");
            Show(ignoreShowAnimation);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            _innerState = InnerState.Idle;
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Arena);
            AudioController.instance.PlayMusic(AudioController.MusicCode.Ranking);
            UpdateScrolls();
            UpdateInfo();

            RxProps.ArenaInfoTuple
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
            if (eval.Exception is { })
            {
                _innerState = InnerState.Idle;
                Find<LoadingScreen>().Close();
                return;
            }

            switch (_innerState)
            {
                case InnerState.EarlyRegistration:
                    _innerState = InnerState.Idle;
                    UpdateBottomButtons();
                    Find<LoadingScreen>().Close();
                    Find<HeaderMenuStatic>()
                        .Show(HeaderMenuStatic.AssetVisibleState.Arena);
                    return;
                case InnerState.RegistrationAndTransitionToArenaBoard:
                    _innerState = InnerState.Idle;
                    var selectedRound = _scroll.SelectedItemData.RoundData;
                    if (eval.Action.championshipId != selectedRound.ChampionshipId ||
                        eval.Action.round != selectedRound.Round)
                    {
                        UpdateBottomButtons();
                        Find<LoadingScreen>().Close();
                        Find<HeaderMenuStatic>()
                            .Show(HeaderMenuStatic.AssetVisibleState.Arena);

                        NotificationSystem.Push(
                            MailType.System,
                            "The round which is you want to join is ended.",
                            NotificationCell.NotificationType.Information);
                        return;
                    }

                    Close();
                    Find<LoadingScreen>().Close();
                    Find<ArenaBoard>().Show(
                        _scroll.SelectedItemData.RoundData,
                        RxProps.ArenaInformationOrderedWithScore.Value);
                    return;
                case InnerState.Idle:
                    UpdateBottomButtons();
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
            var arenaSheet = TableSheets.Instance.ArenaSheet;
            var selectedRoundData = arenaSheet.TryGetCurrentRound(
                Game.Game.instance.Agent.BlockIndex,
                out var outCurrentRoundData)
                ? outCurrentRoundData
                : null;
            var selectedIndex = 0;
            if (selectedRoundData is not null)
            {
                selectedIndex = arenaSheet[selectedRoundData.ChampionshipId].Round
                    .IndexOf(selectedRoundData);
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
                    .Where(e => e.RoundDataBridge.ArenaType == ArenaType.Season)
                    .Select(e => e.RoundDataBridge.Round)
                    .ToArray();
                var arenaDataList = _so.ArenaDataList
                    .Select(data => data.RoundDataBridge.ToRoundData())
                    .ToList();
                return arenaDataList.Select(data => new ArenaJoinSeasonItemData
                {
                    RoundData = data,
                    SeasonNumber = arenaDataList.TryGetSeasonNumber(
                        data.Round,
                        out var seasonNumber)
                        ? seasonNumber
                        : (int?)null,
                    ChampionshipSeasonNumbers =
                        arenaDataList.GetSeasonNumbersOfChampionship(),
                }).ToList();
            }
#endif
            {
                var blockIndex = Game.Game.instance.Agent.BlockIndex;
                var row = TableSheets.Instance.ArenaSheet.GetRowByBlockIndex(blockIndex);
                return row.Round
                    .Select(roundData => new ArenaJoinSeasonItemData
                    {
                        RoundData = roundData,
                        SeasonNumber = row.TryGetSeasonNumber(
                            roundData.Round,
                            out var seasonNumber)
                            ? seasonNumber
                            : (int?)null,
                        ChampionshipSeasonNumbers =
                            row.GetSeasonNumbersOfChampionship(),
                    }).ToList();
            }
        }

        private static IList<ArenaJoinSeasonBarItemData> GetBarScrollData(
            int barScrollCellCount, int barIndexOffset)
        {
            return Enumerable.Range(0, barScrollCellCount)
                .Select(index => new ArenaJoinSeasonBarItemData
                {
                    visible = index == barIndexOffset,
                })
                .ToList();
        }

        private static int ReverseScrollIndex(int scrollIndex) =>
            _barScrollCellCount - scrollIndex - 1;

        private void UpdateInfo()
        {
            var selectedRoundData = _scroll.SelectedItemData.RoundData;
            _info.SetData(
                _scroll.SelectedItemData.GetRoundName(),
                selectedRoundData,
                GetRewardType(_scroll.SelectedItemData),
                selectedRoundData.TryGetMedalItemResourceId(out var medalItemId)
                    ? medalItemId
                    : (int?)null);

            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var row = TableSheets.Instance.ArenaSheet.GetRowByBlockIndex(blockIndex);
            var championshipRound = row.Round
                .Last(roundData => roundData.ArenaType == ArenaType.Championship).Round;
            if (selectedRoundData.Round > championshipRound)
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
            _earlyPaymentButton.OnGoToGrinding
                .Subscribe(_ => GoToGrinding())
                .AddTo(gameObject);
            _earlyPaymentButton.OnJoinArenaAction
                .Subscribe(_ =>
                {
                    _innerState = InnerState.EarlyRegistration;
                    Find<LoadingScreen>().Show(LoadingScreen.LoadingType.Arena);
                })
                .AddTo(gameObject);

            void OnClickJoinButton()
            {
                AudioController.PlayClick();
                if (RxProps.ArenaInfoTuple.HasValue &&
                    RxProps.ArenaInfoTuple.Value.current is { })
                {
                    Close();
                    Find<ArenaBoard>().Show(
                        _scroll.SelectedItemData.RoundData,
                        RxProps.ArenaInformationOrderedWithScore.Value);
                    return;
                }

                _innerState = InnerState.RegistrationAndTransitionToArenaBoard;
                Find<LoadingScreen>().Show(LoadingScreen.LoadingType.Arena);
                var selectedRoundData = _scroll.SelectedItemData.RoundData;
                var itemSlotState = States.Instance.CurrentItemSlotStates[BattleType.Arena];
                var runeInfos = States.Instance.CurrentRuneSlotStates[BattleType.Arena]
                    .GetEquippedRuneSlotInfos();
                ActionManager.Instance
                    .JoinArena(
                        itemSlotState.Costumes,
                        itemSlotState.Equipments,
                        runeInfos,
                        selectedRoundData.ChampionshipId,
                        selectedRoundData.Round)
                    .Subscribe();
            }

            _joinButton.SetState(ConditionalButton.State.Normal);
            _joinButton.OnClickSubject.Subscribe(_ => OnClickJoinButton()).AddTo(gameObject);

            _paymentButton.SetState(ConditionalButton.State.Conditional);
            _paymentButton.SetCondition(() => CheckChampionshipConditions(true));
            _paymentButton.OnClickSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                _innerState = InnerState.RegistrationAndTransitionToArenaBoard;
                var balance = States.Instance.CrystalBalance;
                var cost = _paymentButton.CrystalCost;
                var enoughMessageFormat = L10nManager.Localize("UI_ARENA_JOIN_WITH_CRYSTAL_Q");
                var notEnoughMessage = L10nManager.Localize("UI_NOT_ENOUGH_CRYSTAL");
                Find<PaymentPopup>().Show(
                    CostType.Crystal,
                    balance.MajorUnit,
                    cost,
                    string.Format(enoughMessageFormat, cost),
                    notEnoughMessage,
                    JoinArenaAction,
                    GoToGrinding);
            }).AddTo(gameObject);

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
            var selectedRoundData = _scroll.SelectedItemData.RoundData;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var isOpened = selectedRoundData.IsTheRoundOpened(blockIndex);
            var arenaType = selectedRoundData.ArenaType;
            UpdateJoinAndPaymentButton(arenaType, isOpened);
            var championshipId = selectedRoundData.ChampionshipId;
            UpdateEarlyRegistrationButton(
                arenaType,
                isOpened,
                blockIndex,
                championshipId);
        }

        private void UpdateEarlyRegistrationButton(
            ArenaType arenaType,
            bool isOpened,
            long blockIndex,
            int championshipId)
        {
            switch (arenaType)
            {
                case ArenaType.OffSeason:
                {
                    if (isOpened &&
                        TableSheets.Instance.ArenaSheet.TryGetNextRound(
                            blockIndex,
                            out var next))
                    {
                        var cost = (long)ArenaHelper.GetEntranceFee(
                            next,
                            blockIndex,
                            States.Instance.CurrentAvatarState.level).MajorUnit;
                        if (RxProps.ArenaInfoTuple.Value.next is { })
                        {
                            _earlyPaymentButton.Show(
                                next.ArenaType,
                                next.ChampionshipId,
                                next.Round,
                                true,
                                cost);
                        }
                        else if (next.ArenaType == ArenaType.Championship)
                        {
                            if (TableSheets.Instance.ArenaSheet.IsChampionshipConditionComplete(
                                    championshipId,
                                    States.Instance.CurrentAvatarState))
                            {
                                _earlyPaymentButton.Show(
                                    next.ArenaType,
                                    next.ChampionshipId,
                                    next.Round,
                                    false,
                                    cost);
                            }
                            else
                            {
                                _earlyPaymentButton.Hide();
                            }
                        }
                        else
                        {
                            _earlyPaymentButton.Show(
                                next.ArenaType,
                                next.ChampionshipId,
                                next.Round,
                                false,
                                cost);
                        }
                    }
                    else
                    {
                        _earlyPaymentButton.Hide();
                    }

                    break;
                }
                case ArenaType.Season:
                case ArenaType.Championship:
                {
                    _earlyPaymentButton.Hide();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void UpdateJoinAndPaymentButton(
            ArenaType arenaType,
            bool isOpened)
        {
            switch (arenaType)
            {
                case ArenaType.OffSeason:
                {
                    _bottomButtonText.enabled = false;
                    _joinButton.Interactable = isOpened;
                    _joinButton.gameObject.SetActive(true);
                    _paymentButton.gameObject.SetActive(false);

                    return;
                }
                case ArenaType.Season:
                case ArenaType.Championship:
                {
                    if (isOpened)
                    {
                        if (RxProps.ArenaInfoTuple.Value.current is null)
                        {
                            _joinButton.gameObject.SetActive(false);
                            if (arenaType == ArenaType.Championship &&
                                !CheckChampionshipConditions(false))
                            {
                                _bottomButtonText.text =
                                    L10nManager.Localize("UI_NOT_ENOUGH_ARENA_MEDALS");
                                _bottomButtonText.enabled = true;
                                _paymentButton.gameObject.SetActive(false);

                                return;
                            }

                            _bottomButtonText.enabled = false;

                            var cost = (long)ArenaHelper.GetEntranceFee(
                                _scroll.SelectedItemData.RoundData,
                                Game.Game.instance.Agent.BlockIndex,
                                States.Instance.CurrentAvatarState.level).MajorUnit;
                            _paymentButton.SetCost(CostType.Crystal, cost);
                            _paymentButton.UpdateObjects();
                            _paymentButton.Interactable = true;
                            _paymentButton.gameObject.SetActive(true);

                            return;
                        }

                        _bottomButtonText.enabled = false;
                        _joinButton.Interactable = true;
                        _joinButton.gameObject.SetActive(true);
                        _paymentButton.gameObject.SetActive(false);
                    }
                    else
                    {
                        if (arenaType == ArenaType.Championship &&
                            !CheckChampionshipConditions(false))
                        {
                            _bottomButtonText.text =
                                L10nManager.Localize("UI_NOT_ENOUGH_ARENA_MEDALS");
                            _bottomButtonText.enabled = true;
                            _joinButton.gameObject.SetActive(false);
                            _paymentButton.gameObject.SetActive(false);

                            return;
                        }

                        _bottomButtonText.enabled = false;
                        _joinButton.Interactable = false;
                        _joinButton.gameObject.SetActive(true);
                        _paymentButton.gameObject.SetActive(false);
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool CheckJoinCost()
        {
            var selectedRoundData = _scroll.SelectedItemData.RoundData;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var cost = ArenaHelper.GetEntranceFee(
                selectedRoundData,
                blockIndex,
                States.Instance.CurrentAvatarState.level);
            return States.Instance.CrystalBalance >= cost;
        }

        private bool CheckChampionshipConditions(bool considerCrystal)
        {
            var selectedRoundData = _scroll.SelectedItemData.RoundData;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var row = TableSheets.Instance.ArenaSheet.GetRowByBlockIndex(blockIndex);
            var medalTotalCount = ArenaHelper.GetMedalTotalCount(
                row,
                States.Instance.CurrentAvatarState);
            var completeCondition = medalTotalCount >=
                                    selectedRoundData.RequiredMedalCount;
            if (!considerCrystal)
            {
                return completeCondition;
            }

            return completeCondition && CheckJoinCost();
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
            var row = TableSheets.Instance.ArenaSheet.GetRowByBlockIndex(blockIndex);
            var avatarState = States.Instance.CurrentAvatarState;
            var requiredMedal = row.Round
                .FirstOrDefault(r => r.ArenaType == ArenaType.Championship)
                ?.RequiredMedalCount ?? 0;
            var medalTotalCount = ArenaHelper.GetMedalTotalCount(row, avatarState);
            return (requiredMedal, medalTotalCount);
        }

        private ArenaJoinSeasonInfo.RewardType GetRewardType(ArenaJoinSeasonItemData data)
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                var soData = _so.ArenaDataList.FirstOrDefault(soData =>
                    soData.RoundDataBridge.ChampionshipId == data.RoundData.ChampionshipId &&
                    soData.RoundDataBridge.Round == data.RoundData.Round);
                return soData is null
                    ? ArenaJoinSeasonInfo.RewardType.None
                    : soData.RewardType;
            }
#endif

            return data.RoundData.ArenaType switch
            {
                ArenaType.OffSeason => ArenaJoinSeasonInfo.RewardType.Food,
                ArenaType.Season =>
                    ArenaJoinSeasonInfo.RewardType.Food |
                    ArenaJoinSeasonInfo.RewardType.Medal |
                    ArenaJoinSeasonInfo.RewardType.NCG,
                ArenaType.Championship =>
                    ArenaJoinSeasonInfo.RewardType.Food |
                    ArenaJoinSeasonInfo.RewardType.Medal |
                    ArenaJoinSeasonInfo.RewardType.NCG,
                    // NOTE: Enable costume when championship rewards contains one.
                    // ArenaJoinSeasonInfo.RewardType.Costume,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void JoinArenaAction()
        {
            Find<LoadingScreen>().Show(LoadingScreen.LoadingType.Arena);
            var selectedRoundData = _scroll.SelectedItemData.RoundData;
            var itemSlotState = States.Instance.CurrentItemSlotStates[BattleType.Arena];
            var runeInfos = States.Instance.CurrentRuneSlotStates[BattleType.Arena]
                .GetEquippedRuneSlotInfos();
            ActionManager.Instance
                .JoinArena(
                    itemSlotState.Costumes,
                    itemSlotState.Equipments,
                    runeInfos,
                    selectedRoundData.ChampionshipId,
                    selectedRoundData.Round)
                .Subscribe();
        }

        private void GoToGrinding()
        {
            Close(true);
            Find<Menu>().Close();
            Find<WorldMap>().Close();
            Find<StageInformation>().Close();
            Find<BattlePreparation>().Close();
            Find<Grind>().Show();
        }
        public void TutorialActionSeasonPassGuidePopup()
        {
            Widget.Find<SeasonPassNewPopup>().Show();
        }
    }
}
