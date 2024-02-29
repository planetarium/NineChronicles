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
            public readonly ReactiveProperty<int> StageIdToShow = new ReactiveProperty<int>(0);
            public readonly ReactiveProperty<int> PageCount = new ReactiveProperty<int>(0);
            public readonly ReactiveProperty<int> CurrentPageNumber = new ReactiveProperty<int>(0);

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
        private List<WorldMapPage> pages = null;

        [SerializeField]
        private List<Toggle> toggles = null;

        [SerializeField]
        private Button previousButton = null;

        [SerializeField]
        private Button nextButton = null;

        [SerializeField]
        private Transform content = null;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

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
                        _ => worldRow.Id.ToString("00"),
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
                return;

            var toggle = toggles[pageNumber - 1];
            toggle.isOn = false;
            toggle.isOn = true;
        }
    }
}
