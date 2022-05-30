using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.Arena;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Model.EnumType;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Arena.Join;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class ArenaJoin : Widget
    {
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
        private int _barPointCount;

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

        private readonly List<IDisposable> _disposablesForShow = new List<IDisposable>();

        protected override void Awake()
        {
            base.Awake();

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
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Arena);
            UpdateScrolls(_disposablesForShow);
            UpdateInfo();
            UpdateBottomButtons();

            _scroll.OnSelectionChanged
                .Select(ReverseScrollIndex)
                .Subscribe(reversedIndex =>
                {
                    _barScroll.SelectCell(reversedIndex, false);
                    UpdateInfo();
                    UpdateBottomButtons();
                })
                .AddTo(_disposablesForShow);
            _barScroll.OnSelectionChanged
                .Select(ReverseScrollIndex)
                .Subscribe(reversedIndex =>
                {
                    _scroll.SelectCell(reversedIndex, false);
                    UpdateInfo();
                    UpdateBottomButtons();
                })
                .AddTo(_disposablesForShow);
            RxProps.ArenaInfoTuple
                .Subscribe(tuple => UpdateBottomButtons())
                .AddTo(_disposablesForShow);
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesForShow.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        private void UpdateScrolls(IList<IDisposable> disposables)
        {
            var scrollData = GetScrollData();
            var selectedRoundData = TableSheets.Instance.ArenaSheet.TryGetCurrentRound(
                Game.Game.instance.Agent.BlockIndex,
                out var outCurrentRoundData)
                ? outCurrentRoundData
                : null;
            var selectedIndex = selectedRoundData?.Round - 1 ?? 0;
            _scroll.SetData(scrollData, selectedIndex);
            var barIndexOffset = (int)math.ceil(_barPointCount / 2f) - 1;
            _barScroll.SetData(
                GetBarScrollData(barIndexOffset),
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
                        data.ArenaType == ArenaType.Championship
                            ? championshipSeasonIds
                            : Array.Empty<int>(),
                }).ToList();
            }
#endif
            {
                var blockIndex = Game.Game.instance.Agent.BlockIndex;
                var row = TableSheets.Instance.ArenaSheet.GetRowByBlockIndex(blockIndex);
                var championshipSeasonIds = row.Round
                    .Where(e => e.ArenaType == ArenaType.Season)
                    .Select(e => e.Round)
                    .ToArray();
                return row.Round
                    .Select(roundData => new ArenaJoinSeasonItemData
                    {
                        RoundData = roundData,
                        SeasonNumber = row.TryGetSeasonNumber(roundData.Round, out var seasonNumber)
                            ? seasonNumber
                            : (int?)null,
                        ChampionshipSeasonNumbers = roundData.ArenaType == ArenaType.Championship
                            ? championshipSeasonIds
                            : Array.Empty<int>(),
                    }).ToList();
            }
        }

        private IList<ArenaJoinSeasonBarItemData> GetBarScrollData(
            int barIndexOffset)
        {
            var cellCount = _barPointCount;
            return Enumerable.Range(0, cellCount)
                .Select(index => new ArenaJoinSeasonBarItemData
                {
                    visible = index == barIndexOffset,
                })
                .ToList();
        }

        private int ReverseScrollIndex(int scrollIndex) =>
            _barPointCount - scrollIndex - 1;

        private void UpdateInfo()
        {
            var selectedRoundData = _scroll.SelectedItemData.RoundData;
            _info.SetData(
                _scroll.SelectedItemData.GetRoundName(),
                GetSeasonProgress(selectedRoundData),
                GetConditions(),
                GetRewardType(_scroll.SelectedItemData),
                selectedRoundData.TryGetMedalItemId(out var medalItemId)
                    ? medalItemId
                    : (int?)null);
        }

        private void InitializeBottomButtons()
        {
            _joinButton.SetState(ConditionalButton.State.Normal);
            _paymentButton.SetState(ConditionalButton.State.Conditional);

            _joinButton.OnClickSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                Find<ArenaBoard>()
                    .ShowAsync(_scroll.SelectedItemData.RoundData)
                    .Forget();
                Close();
            }).AddTo(gameObject);
            _paymentButton.OnClickSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                Find<ArenaBoard>()
                    .ShowAsync(_scroll.SelectedItemData.RoundData)
                    .Forget();
                Close();
            }).AddTo(gameObject);
        }
        
        private void UpdateBottomButtons()
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var selectedRoundData = _scroll.SelectedItemData.RoundData;
            var isOpened = selectedRoundData.IsTheRoundOpened(blockIndex);
            switch (selectedRoundData.ArenaType)
            {
                case ArenaType.OffSeason:
                {
                    if (isOpened&&
                        TableSheets.Instance.ArenaSheet.TryGetNextRound(
                            blockIndex,
                            out var next))
                    {
                        var isRegisteredNextRound = RxProps.ArenaInfoTuple.Value.next is { };
                        _earlyPaymentButton.Show(
                            next.ArenaType,
                            next.ChampionshipId,
                            next.Round,
                            isRegisteredNextRound,
                            next.DiscountedEntranceFee);
                    }
                    else
                    {
                        _earlyPaymentButton.Hide();
                    }

                    _joinButton.Interactable = isOpened;
                    _joinButton.gameObject.SetActive(true);
                    _paymentButton.gameObject.SetActive(false);
                    break;
                }
                case ArenaType.Season:
                case ArenaType.Championship:
                {
                    _earlyPaymentButton.Hide();
                    
                    if (isOpened)
                    {
                        if (RxProps.ArenaInfoTuple.Value.current is null)
                        {
                            _joinButton.gameObject.SetActive(false);
                            var cost = (int)selectedRoundData.EntranceFee;
                            _paymentButton.SetCondition(() =>
                                States.Instance.CrystalBalance >=
                                cost * States.Instance.CrystalBalance.Currency);
                            _paymentButton.SetCost(CostType.Crystal, cost);
                            _paymentButton.UpdateObjects();
                            _paymentButton.Interactable =
                                States.Instance.CrystalBalance >=
                                cost * States.Instance.CrystalBalance.Currency;
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

        private static (long beginning, long end, long current) GetSeasonProgress(
            ArenaSheet.RoundData selectedRoundData)
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            return (
                selectedRoundData.StartBlockIndex,
                selectedRoundData.EndBlockIndex,
                blockIndex);
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
