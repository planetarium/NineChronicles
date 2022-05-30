using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Arena;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
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
        private Button _earlyPaymentButton;

        [SerializeField]
        private Button _backButton;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        protected override void Awake()
        {
            base.Awake();

            _backButton.OnClickAsObservable().Subscribe(_ =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            }).AddTo(gameObject);
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
            _earlyPaymentButton.onClick.AsObservable().Subscribe().AddTo(gameObject);

            CloseWidget = () =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            };
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Arena);
            InitializeScrolls(_disposables);
            UpdateInfo();
            UpdateButtons();
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposables.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        private void InitializeScrolls(IList<IDisposable> disposables)
        {
            var scrollData = GetScrollData();
            var selectedIndex = 0;
            _scroll.SetData(scrollData, selectedIndex);
            var barIndexOffset = (int)math.ceil(_barPointCount / 2f) - 1;
            _barScroll.SetData(
                GetBarScrollData(barIndexOffset),
                ReverseScrollIndex(selectedIndex));

            // NOTE: Scroll events should subscribe after set data. 
            _scroll.OnSelectionChanged
                .Select(ReverseScrollIndex)
                .Subscribe(reversedIndex =>
                {
                    _barScroll.SelectCell(reversedIndex, false);
                    UpdateInfo();
                    UpdateButtons();
                })
                .AddTo(disposables);
            _barScroll.OnSelectionChanged
                .Select(ReverseScrollIndex)
                .Subscribe(reversedIndex =>
                {
                    _scroll.SelectCell(reversedIndex, false);
                    UpdateInfo();
                    UpdateButtons();
                })
                .AddTo(disposables);
        }

        private IList<ArenaJoinSeasonItemData> GetScrollData()
        {
#if UNITY_EDITOR
            if (_useSo && _so)
            {
                int? GetSeasonNumber(
                    IList<ArenaJoinSO.ArenaData> list,
                    ArenaJoinSO.RoundDataBridge data)
                {
                    var seasonNumber = 0;
                    foreach (var arenaData in list)
                    {
                        if (arenaData.RoundDataBridge.ArenaType == ArenaType.Season)
                        {
                            seasonNumber++;
                        }

                        if (arenaData.RoundDataBridge.Round == data.Round)
                        {
                            return arenaData.RoundDataBridge.ArenaType == ArenaType.Season
                                ? seasonNumber
                                : (int?)null;
                        }
                    }

                    return null;
                }

                var championshipSeasonIds = _so.ArenaDataList
                    .Where(e => e.RoundDataBridge.ArenaType == ArenaType.Season)
                    .Select(e => e.RoundDataBridge.Round)
                    .ToArray();
                return _so.ArenaDataList
                    .Select(data => new ArenaJoinSeasonItemData
                    {
                        RoundData = data.RoundDataBridge.ToRoundData(),
                        SeasonNumber =
                            GetSeasonNumber(_so.ArenaDataList, data.RoundDataBridge),
                        ChampionshipSeasonNumbers =
                            data.RoundDataBridge.ArenaType == ArenaType.Championship
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

        private void UpdateButtons()
        {
            // TODO: 아레나 라운드 정보에 따라 버튼 상태를 갱신한다.
            _joinButton.gameObject.SetActive(true);
            _paymentButton.gameObject.SetActive(false);
            _earlyPaymentButton.gameObject.SetActive(false);
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
