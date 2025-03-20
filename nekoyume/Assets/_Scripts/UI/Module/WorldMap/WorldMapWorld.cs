using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Event;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using EventType = Nekoyume.EnumType.EventType;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class WorldMapWorld : MonoBehaviour
    {
        public class ViewModel : IDisposable
        {
            public readonly WorldSheet.Row RowData;
            public readonly StageType StageType;
            public readonly ReactiveProperty<int> StageIdToShow = new(0);
            public readonly ReactiveProperty<int> PageCount = new(0);
            public readonly ReactiveProperty<int> CurrentPageNumber = new(0);

            public ViewModel(WorldSheet.Row rowData, StageType stageType)
            {
                RowData = rowData;
                StageType = stageType;
            }

            public void Dispose()
            {
                CurrentPageNumber.Dispose();
            }
        }

        [SerializeField]
        private HorizontalScrollSnap horizontalScrollSnap = null;

        [SerializeField]
        private WorldMapPage pagePrefab = null;

        [SerializeField]
        private Toggle togglePrefab = null;

        [SerializeField]
        private List<WorldMapPage> pages = null;

        [SerializeField]
        private List<Toggle> toggles = null;

        [SerializeField]
        private Button previousButton = null;

        [SerializeField]
        private Button nextButton = null;

        [SerializeField]
        private Transform content = null;

        private readonly List<IDisposable> _disposablesForModel = new();

        public IReadOnlyList<WorldMapPage> Pages => pages;

        public ViewModel SharedViewModel { get; private set; }

        private void Awake()
        {
            previousButton.OnClickAsObservable()
                .Subscribe(_ => AudioController.PlayClick())
                .AddTo(gameObject);
            nextButton.OnClickAsObservable()
                .Subscribe(_ => AudioController.PlayClick())
                .AddTo(gameObject);

            horizontalScrollSnap.OnSelectionPageChangedEvent.AddListener(value =>
            {
                var pageNumber = Mathf.Clamp(value + 1, 1, SharedViewModel.PageCount.Value);
                SharedViewModel.CurrentPageNumber.SetValueAndForceNotify(pageNumber);
                ToggleOn(pageNumber);
            });

            foreach (var stage in pages.SelectMany(page => page.Stages))
            {
                stage.onClick.Subscribe(SubscribeOnClick)
                    .AddTo(gameObject);
            }

            foreach (var (toggle, index) in toggles.Select((toggle, index) => (toggle, index)))
            {
                toggle.onValueChanged.AddListener(value =>
                {
                    if (value)
                    {
                        if (SharedViewModel.CurrentPageNumber.Value != index + 1)
                        {
                            horizontalScrollSnap.ChangePage(index);
                        }

                        SharedViewModel.CurrentPageNumber.SetValueAndForceNotify(
                            Mathf.Clamp(index + 1, 1, SharedViewModel.PageCount.Value));
                    }
                });
            }
        }

        public void Set(WorldSheet.Row worldRow, StageType stageType)
        {
            if (worldRow is null)
            {
                throw new ArgumentNullException(nameof(worldRow));
            }

            _disposablesForModel.DisposeAllAndClear();
            SharedViewModel = new ViewModel(worldRow, stageType);
            var stageTuples = TableSheets.Instance.StageWaveSheet.OrderedList
                .Where(row => row.StageId >= worldRow.StageBegin &&
                    row.StageId <= worldRow.StageEnd)
                .Select(row => (row.StageId, row.HasBoss))
                .ToList();
            if (worldRow.StagesCount != stageTuples.Count)
            {
                throw new SheetRowValidateException(
                    $"{worldRow.Id}:" +
                    $" worldRow.StagesCount({worldRow.StagesCount}) != stageRowsCount({stageTuples.Count})");
            }

            Set(worldRow, stageType, stageTuples);
        }

        public void Set(EventDungeonSheet.Row eventDungeonRow)
        {
            if (eventDungeonRow is null)
            {
                throw new ArgumentNullException(nameof(eventDungeonRow));
            }

            _disposablesForModel.DisposeAllAndClear();
            SharedViewModel = new ViewModel(eventDungeonRow, StageType.EventDungeon);
            var eventDungeonStageTuples = RxProps.EventDungeonStageWaveRows
                .Select(row => (row.StageId, row.HasBoss))
                .ToList();
            if (eventDungeonRow.StagesCount != eventDungeonStageTuples.Count)
            {
                throw new SheetRowValidateException(
                    $"{eventDungeonRow.Id}:" +
                    $" worldRow.StagesCount({eventDungeonRow.StagesCount}) != stageRowsCount({eventDungeonStageTuples.Count})");
            }

            Set(eventDungeonRow, StageType.EventDungeon, eventDungeonStageTuples);
        }

        private void Set(
            WorldSheet.Row worldRow,
            StageType stageType,
            List<(int stageId, bool hasBoss)> stageTuples)
        {
            var eventType = stageType == StageType.EventDungeon ? EventManager.GetEventInfo().EventType : EventType.Default;

            var stageWaveRowsCount = stageTuples.Count;
            
            int requiredPageCount = CalculateRequiredPageCount(stageWaveRowsCount);
            
            EnsureEnoughPagesAndToggles(requiredPageCount);
            
            var stageOffset = 0;
            var nextPageShouldHide = false;
            var pageIndex = 1;
            foreach (var page in pages)
            {
                page.gameObject.SetActive(false);
                page.transform.SetParent(transform);

                if (nextPageShouldHide)
                {
                    continue;
                }

                page.transform.SetParent(content);
                page.gameObject.SetActive(true);

                var stageCount = page.Stages.Count;
                var stageModels = new List<WorldMapStage.ViewModel>();
                for (var i = 0; i < stageCount; i++)
                {
                    if (nextPageShouldHide)
                    {
                        stageModels.Add(new WorldMapStage.ViewModel(
                            SharedViewModel.StageType,
                            WorldMapStage.State.Hidden));

                        continue;
                    }

                    var stageWaveRowsIndex = stageOffset + i;
                    if (stageWaveRowsIndex < stageWaveRowsCount)
                    {
                        var stageTuple = stageTuples[stageWaveRowsIndex];
                        var stageModel = new WorldMapStage.ViewModel(
                            SharedViewModel.StageType,
                            stageTuple.stageId,
                            GetBossType(stageTuple.stageId, stageTuple.hasBoss),
                            eventType,
                            WorldMapStage.State.Normal);

                        stageModels.Add(stageModel);
                    }
                    else
                    {
                        nextPageShouldHide = true;
                        stageModels.Add(new WorldMapStage.ViewModel(
                            SharedViewModel.StageType,
                            WorldMapStage.State.Hidden));
                    }
                }

                var background = SpriteHelper.GetWorldMapBackground(
                    stageType switch
                    {
                        StageType.Mimisbrunnr => "mimisbrunnr",
                        StageType.EventDungeon => EventManager.GetEventInfo().EventType.ToString(),
                        _ => worldRow.Id.ToString("00")
                    },
                    pageIndex);
                page.Show(stageModels, background);
                pageIndex += 1;
                stageOffset += stageModels.Count;
                if (stageOffset >= stageWaveRowsCount)
                {
                    nextPageShouldHide = true;
                }

                // todo: Remove this method, Read data from sheet
                BossType GetBossType(int stageId, bool hasBoss)
                {
                    if (!hasBoss)
                    {
                        return BossType.None;
                    }

                    if (stageType == StageType.EventDungeon)
                    {
                        return stageId % 20 == 0
                            ? BossType.LastBoss
                            : BossType.MiddleBoss;
                    }

                    return stageId % 50 == 0
                        ? BossType.LastBoss
                        : BossType.MiddleBoss;
                }
            }

            SharedViewModel.StageIdToShow.Value = worldRow.StageBegin + stageWaveRowsCount - 1;
            SharedViewModel.PageCount.Value = pages.Count(p => p.gameObject.activeSelf);

            for (var i = 0; i < toggles.Count; i++)
            {
                toggles[i].gameObject.SetActive(i < SharedViewModel.PageCount.Value);
            }

            SharedViewModel.CurrentPageNumber
                .Subscribe(currentPageNumber =>
                {
                    previousButton.gameObject.SetActive(currentPageNumber > 1);
                    nextButton.gameObject.SetActive(
                        currentPageNumber < SharedViewModel.PageCount.Value);
                })
                .AddTo(_disposablesForModel);

            horizontalScrollSnap.ChangePage(SharedViewModel.CurrentPageNumber.Value - 1);
        }

        private int CalculateRequiredPageCount(int stageCount)
        {
            if (pages == null || pages.Count == 0 || pages[0] == null || pages[0].Stages == null || pages[0].Stages.Count == 0)
            {
                return 0;
            }
            
            int stagesPerPage = pages[0].Stages.Count;
            return Mathf.CeilToInt((float)stageCount / stagesPerPage);
        }

        private void EnsureEnoughPagesAndToggles(int requiredCount)
        {
            // 1. 필요한 경우 페이지 추가
            while (pages.Count < requiredCount)
            {
                if (pagePrefab != null)
                {
                    WorldMapPage newPage = Instantiate(pagePrefab, content);
                    pages.Add(newPage);
                    
                    // 새 페이지의 스테이지 버튼에 이벤트 리스너 추가
                    foreach (var stage in newPage.Stages)
                    {
                        // WorldMapWorld의 이벤트 핸들러 추가
                        stage.onClick.Subscribe(SubscribeOnClick)
                            .AddTo(gameObject);
                        
                        // StageInformation 위젯에 이벤트 연결
                        ConnectStageToStageInformation(stage);
                    }
                }
                else
                {
                    NcDebug.LogError("페이지 프리팹이 설정되지 않았습니다.");
                    break;
                }
            }
            
            // 2. 필요한 만큼만 페이지 활성화, 나머지는 비활성화
            for (int i = 0; i < pages.Count; i++)
            {
                // 요구사항보다 많은 페이지는 완전히 비활성화
                if (i >= requiredCount)
                {
                    pages[i].gameObject.SetActive(false);
                    pages[i].transform.SetParent(transform); // 부모를 content에서 제거
                }
            }
            
            // 3. 필요한 경우 토글 추가
            Transform toggleParent = toggles.Count > 0 ? toggles[0].transform.parent : transform;
            while (toggles.Count < requiredCount)
            {
                if (togglePrefab != null)
                {
                    Toggle newToggle = Instantiate(togglePrefab, toggleParent);
                    int index = toggles.Count;
                    
                    newToggle.onValueChanged.AddListener(value =>
                    {
                        if (value)
                        {
                            if (SharedViewModel.CurrentPageNumber.Value != index + 1)
                            {
                                horizontalScrollSnap.ChangePage(index);
                            }

                            SharedViewModel.CurrentPageNumber.SetValueAndForceNotify(
                                Mathf.Clamp(index + 1, 1, SharedViewModel.PageCount.Value));
                        }
                    });
                    
                    toggles.Add(newToggle);
                }
                else
                {
                    NcDebug.LogError("토글 프리팹이 설정되지 않았습니다.");
                    break;
                }
            }
            
            // 4. 필요한 만큼만 토글 활성화, 나머지는 비활성화
            for (int i = 0; i < toggles.Count; i++)
            {
                toggles[i].gameObject.SetActive(i < requiredCount);
            }
            
            // 5. HorizontalScrollSnap을 업데이트하여 페이지 수 반영
            if (horizontalScrollSnap != null)
            {
                // 활성화된 페이지 수에 맞게 HorizontalScrollSnap 업데이트
                horizontalScrollSnap.UpdateLayout();
            }
        }

        // StageInformation 위젯에 스테이지 이벤트 연결
        private void ConnectStageToStageInformation(WorldMapStage stage)
        {
            var stageInfo = Widget.Find<StageInformation>();
            if (stageInfo != null)
            {
                // StageInformation 위젯의 SharedViewModel에 스테이지 이벤트 연결
                stage.onClick.Subscribe(worldMapStage =>
                {
                    stageInfo.SelectStage(worldMapStage);
                }).AddTo(stageInfo.gameObject);
            }
        }

        public void Set(int openedStageId = -1, int selectedStageId = -1)
        {
            foreach (var stage in pages
                .SelectMany(page => page.Stages)
                .Where(stage => stage.SharedViewModel is not null))
            {
                var stageId = stage.SharedViewModel.stageId;
                var stageState = WorldMapStage.State.Normal;
                if (stageId < SharedViewModel.RowData.StageBegin ||
                    stageId > SharedViewModel.StageIdToShow.Value)
                {
                    stageState = WorldMapStage.State.Hidden;
                }
                else if (stageId > openedStageId)
                {
                    stageState = WorldMapStage.State.Disabled;
                }

                stage.SharedViewModel.State.Value = stageState;
                stage.SharedViewModel.Selected.Value = stageId == selectedStageId;
            }
        }

        public void ShowByStageId(int value, int stageIdToNotify)
        {
            var pageNumber = GetPageNumber(value);
            SharedViewModel.CurrentPageNumber.SetValueAndForceNotify(pageNumber);
            horizontalScrollSnap.ChangePage(pageNumber - 1);
            horizontalScrollSnap.StartingScreen = pageNumber - 1;
            SetSelectedStageId(value, stageIdToNotify);
            gameObject.SetActive(true);
        }

        private int GetPageNumber(int stageId)
        {
            var pageNumber = 1;
            var stageOffset = SharedViewModel.RowData.StageBegin - 1;
            foreach (var page in pages)
            {
                stageOffset += page.Stages.Count;
                if (stageId > stageOffset &&
                    pageNumber < pages.Count)
                {
                    pageNumber++;

                    continue;
                }

                break;
            }

            return pageNumber;
        }

        private void SetSelectedStageId(int value, int stageIdToNotify)
        {
            foreach (var stage in pages.Where(p => p.gameObject.activeSelf)
                .SelectMany(page => page.Stages))
            {
                var stageId = stage.SharedViewModel.stageId;
                stage.SharedViewModel.Selected.Value = stageId == value;
                stage.SharedViewModel.HasNotification.Value =
                    stageId == stageIdToNotify && !stage.SharedViewModel.Selected.Value;
            }
        }

        private void SubscribeOnClick(WorldMapStage stage)
        {
            SetSelectedStageId(stage.SharedViewModel.stageId,
                Widget.Find<WorldMap>().StageIdToNotify);
        }

        private void ToggleOn(int pageNumber)
        {
            if (toggles.Count < pageNumber)
            {
                return;
            }

            var toggle = toggles[pageNumber - 1];
            toggle.isOn = false;
            toggle.isOn = true;
        }
    }
}
