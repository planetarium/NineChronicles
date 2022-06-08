using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Arena;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Arena.Join;
using Nekoyume.UI.Scroller;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class ArenaJoin : Widget
    {
        private enum InnerState
        {
            Idle,
            EarlyRegistration,
            RegistrationAndTransitionToArenaBoard,
        }

        private const int BarScrollCellCount = 8;
        private static readonly int BarScrollIndexOffset = (int)math.ceil(BarScrollCellCount / 2f) - 1;

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
        private ConditionalButton _joinButton;

        [SerializeField]
        private ConditionalCostButton _paymentButton;

        [SerializeField]
        private ArenaJoinEarlyRegisterButton _earlyPaymentButton;

        [SerializeField]
        private Button _backButton;

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

        public override void Show(bool ignoreShowAnimation = false)
        {
            _innerState = InnerState.Idle;
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Arena);
            UpdateScrolls();
            UpdateInfo();

            // NOTE: RxProp invoke on next callback when subscribe function invoked.
            RxProps.ArenaInfoTuple
                .Subscribe(tuple => UpdateBottomButtons())
                .AddTo(_disposablesForShow);
            base.Show(ignoreShowAnimation);
        }

        public void OnRenderJoinArena(ActionBase.ActionEvaluation<JoinArena> eval)
        {
            if (eval.Exception is { })
            {
                Find<LoadingScreen>().Close();
                return;
            }

            switch (_innerState)
            {
                case InnerState.EarlyRegistration:
                    _innerState = InnerState.Idle;
                    UpdateBottomButtons();
                    Find<LoadingScreen>().Close();
                    return;
                case InnerState.RegistrationAndTransitionToArenaBoard:
                    _innerState = InnerState.Idle;
                    var selectedRound = _scroll.SelectedItemData.RoundData;
                    if (eval.Action.championshipId != selectedRound.ChampionshipId ||
                        eval.Action.round != selectedRound.Round)
                    {
                        UpdateBottomButtons();
                        Find<LoadingScreen>().Close();
                        
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
                        RxProps.ArenaParticipantsOrderedWithScore.Value);
                    return;
                case InnerState.Idle:
                    UpdateBottomButtons();
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesForShow.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
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
            var selectedRoundData = TableSheets.Instance.ArenaSheet.TryGetCurrentRound(
                Game.Game.instance.Agent.BlockIndex,
                out var outCurrentRoundData)
                ? outCurrentRoundData
                : null;
            var selectedIndex = selectedRoundData?.Round - 1 ?? 0;
            _scroll.SetData(scrollData, selectedIndex);
            _barScroll.SetData(
                GetBarScrollData(BarScrollIndexOffset),
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
                    }).ToList();
            }
        }

        private static IList<ArenaJoinSeasonBarItemData> GetBarScrollData(
            int barIndexOffset)
        {
            return Enumerable.Range(0, BarScrollCellCount)
                .Select(index => new ArenaJoinSeasonBarItemData
                {
                    visible = index == barIndexOffset,
                })
                .ToList();
        }

        private static int ReverseScrollIndex(int scrollIndex) =>
            BarScrollCellCount - scrollIndex - 1;

        private void UpdateInfo()
        {
            var selectedRoundData = _scroll.SelectedItemData.RoundData;
            _info.SetData(
                _scroll.SelectedItemData.GetRoundName(),
                selectedRoundData,
                GetConditions(),
                GetRewardType(_scroll.SelectedItemData),
                selectedRoundData.TryGetMedalItemResourceId(out var medalItemId)
                    ? medalItemId
                    : (int?)null);
        }

        /// <summary>
        /// Used from Awake() function once.
        /// </summary>
        private void InitializeBottomButtons()
        {
            _joinButton.SetState(ConditionalButton.State.Normal);
            _paymentButton.SetState(ConditionalButton.State.Conditional);

            _earlyPaymentButton.OnJoinArenaAction
                .Subscribe(_ =>
                {
                    _innerState = InnerState.EarlyRegistration;
                    Find<LoadingScreen>().Show();
                })
                .AddTo(gameObject);

            _joinButton.OnClickSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                if (_scroll.SelectedItemData.RoundData.ArenaType == ArenaType.OffSeason &&
                    RxProps.ArenaInfoTuple.HasValue &&
                    RxProps.ArenaInfoTuple.Value.current is { })
                {
                    Close();
                    Find<ArenaBoard>()
                        .ShowAsync(_scroll.SelectedItemData.RoundData)
                        .Forget();
                    return;
                }

                _innerState = InnerState.RegistrationAndTransitionToArenaBoard;
                Find<LoadingScreen>().Show();
                var inventory = States.Instance.CurrentAvatarState.inventory;
                var selectedRoundData = _scroll.SelectedItemData.RoundData;
                ActionManager.Instance.JoinArena(
                        inventory.Costumes
                            .Where(e => e.Equipped)
                            .Select(e => e.NonFungibleId)
                            .ToList(),
                        inventory.Equipments
                            .Where(e => e.Equipped)
                            .Select(e => e.NonFungibleId)
                            .ToList(),
                        selectedRoundData.ChampionshipId,
                        selectedRoundData.Round)
                    .Subscribe();
            }).AddTo(gameObject);

            _paymentButton.OnClickSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                _innerState = InnerState.RegistrationAndTransitionToArenaBoard;
                Find<LoadingScreen>().Show();
                var inventory = States.Instance.CurrentAvatarState.inventory;
                var selectedRoundData = _scroll.SelectedItemData.RoundData;
                ActionManager.Instance.JoinArena(
                        inventory.Costumes
                            .Where(e => e.Equipped)
                            .Select(e => e.NonFungibleId)
                            .ToList(),
                        inventory.Equipments
                            .Where(e => e.Equipped)
                            .Select(e => e.NonFungibleId)
                            .ToList(),
                        selectedRoundData.ChampionshipId,
                        selectedRoundData.Round)
                    .Subscribe();
            }).AddTo(gameObject);
        }

        private void UpdateBottomButtons()
        {
            var selectedRoundData = _scroll.SelectedItemData.RoundData;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var isOpened = selectedRoundData.IsTheRoundOpened(blockIndex);
            var arenaType = selectedRoundData.ArenaType;
            var championshipId = selectedRoundData.ChampionshipId;
            var crystal = (int)selectedRoundData.EntranceFee;
            UpdateEarlyRegistrationButton(arenaType, isOpened, blockIndex, championshipId);
            UpdateJoinAndPaymentButton(arenaType, isOpened, crystal);
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
                        if (RxProps.ArenaInfoTuple.Value.next is { })
                        {
                            _earlyPaymentButton.Show(
                                next.ArenaType,
                                next.ChampionshipId,
                                next.Round,
                                true,
                                next.DiscountedEntranceFee);
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
                                    next.DiscountedEntranceFee);
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
                                next.DiscountedEntranceFee);
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
            bool isOpened,
            int crystal)
        {
            switch (arenaType)
            {
                case ArenaType.OffSeason:
                {
                    _joinButton.Interactable = isOpened;
                    _joinButton.gameObject.SetActive(true);
                    _paymentButton.gameObject.SetActive(false);
                    break;
                }
                case ArenaType.Season:
                case ArenaType.Championship:
                {
                    if (isOpened)
                    {
                        if (RxProps.ArenaInfoTuple.Value.current is null)
                        {
                            _joinButton.gameObject.SetActive(false);
                            _paymentButton.SetCondition(CheckChampionshipConditions);
                            _paymentButton.SetCost(CostType.Crystal, crystal);
                            _paymentButton.UpdateObjects();
                            _paymentButton.Interactable = CheckChampionshipConditions();
                            _paymentButton.gameObject.SetActive(true);
                        }
                        else
                        {
                            _joinButton.Interactable = true;
                            _joinButton.gameObject.SetActive(true);
                            _paymentButton.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
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

        private bool CheckChampionshipConditions()
        {
            var selectedRoundData = _scroll.SelectedItemData.RoundData;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var row = TableSheets.Instance.ArenaSheet.GetRowByBlockIndex(blockIndex);
            var medalTotalCount = ArenaHelper.GetMedalTotalCount(
                row,
                States.Instance.CurrentAvatarState);
            var completeCondition = medalTotalCount >=
                                    selectedRoundData.RequiredMedalCount;
            var cost = ArenaHelper.GetEntranceFee(
                selectedRoundData,
                blockIndex);
            var hasCost = States.Instance.CrystalBalance >= cost;
            return completeCondition && hasCost;
        }

        private (int max, int current) GetConditions()
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
            var medalTotalCount = ArenaHelper.GetMedalTotalCount(row, avatarState);
            return (row.Round[7].RequiredMedalCount, medalTotalCount);
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
                    ArenaJoinSeasonInfo.RewardType.NCG |
                    ArenaJoinSeasonInfo.RewardType.Costume,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
