using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Quest;
using Nekoyume.TableData;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class WorldMapWorld : MonoBehaviour
    {
        public class ViewModel : IDisposable
        {
            public readonly WorldSheet.Row RowData;
            public readonly ReactiveProperty<int> StageIdToShow = new ReactiveProperty<int>(0);
            public readonly ReactiveProperty<int> PageCount = new ReactiveProperty<int>(0);
            public readonly ReactiveProperty<int> CurrentPageNumber = new ReactiveProperty<int>(0);

            public ViewModel(WorldSheet.Row rowData)
            {
                RowData = rowData;
            }

            public void Dispose()
            {
                CurrentPageNumber.Dispose();
            }
        }

        [SerializeField] private HorizontalScrollSnap horizontalScrollSnap = null;
        [SerializeField] private List<WorldMapPage> pages = null;
        [SerializeField] private List<Toggle> toggles = null;
        [SerializeField] private Button previousButton = null;
        [SerializeField] private Button nextButton = null;

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

        public void Set(WorldSheet.Row worldRow)
        {
            if (worldRow is null)
            {
                throw new ArgumentNullException(nameof(worldRow));
            }

            _disposablesForModel.DisposeAllAndClear();
            SharedViewModel = new ViewModel(worldRow);

            var stageRows = Game.Game.instance.TableSheets.StageWaveSheet.Values
                .Where(stageRow => stageRow.StageId >= worldRow.StageBegin &&
                                   stageRow.StageId <= worldRow.StageEnd)
                .ToList();
            var stageRowsCount = stageRows.Count;
            if (worldRow.StagesCount != stageRowsCount)
            {
                throw new SheetRowValidateException(
                    $"{worldRow.Id}: worldRow.StagesCount({worldRow.StagesCount}) != stageRowsCount({stageRowsCount})");
            }

            var stageOffset = 0;
            var nextPageShouldHide = false;
            var pageIndex = 1;
            foreach (var page in pages)
            {
                page.gameObject.SetActive(false);
                if (nextPageShouldHide)
                {
                    continue;
                }
                page.gameObject.SetActive(true);

                var stageCount = page.Stages.Count;
                var stageModels = new List<WorldMapStage.ViewModel>();
                for (var i = 0; i < stageCount; i++)
                {
                    if (nextPageShouldHide)
                    {
                        stageModels.Add(new WorldMapStage.ViewModel(WorldMapStage.State.Hidden));

                        continue;
                    }

                    var stageRowsIndex = stageOffset + i;
                    if (stageRowsIndex < stageRowsCount)
                    {
                        var stageRow = stageRows[stageRowsIndex];
                        var stageModel = new WorldMapStage.ViewModel(
                            stageRow,
                            stageRow.StageId.ToString(),
                            WorldMapStage.State.Normal);

                        stageModels.Add(stageModel);
                    }
                    else
                    {
                        nextPageShouldHide = true;
                        stageModels.Add(new WorldMapStage.ViewModel(WorldMapStage.State.Hidden));
                    }
                }

                var imageKey = worldRow.Id == GameConfig.MimisbrunnrWorldId ? "99" : $"{worldRow.Id:D2}";
                page.Show(stageModels, imageKey, worldRow.Id == GameConfig.MimisbrunnrWorldId ? 1 : pageIndex);
                pageIndex += 1;
                stageOffset += stageModels.Count;
                if (stageOffset >= stageRowsCount)
                {
                    nextPageShouldHide = true;
                }
            }

            SharedViewModel.StageIdToShow.Value = worldRow.StageBegin + stageRowsCount - 1;
            SharedViewModel.PageCount.Value = pages.Count(p => p.gameObject.activeSelf);

            for (var i = 0; i < toggles.Count; i++)
            {
                toggles[i].gameObject.SetActive(i < SharedViewModel.PageCount.Value);
            }
            
            SharedViewModel.CurrentPageNumber.Value = 1;

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
                .Where(stage => !(stage.SharedViewModel is null)))
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
            foreach (var stage in pages.Where(p => p.gameObject.activeSelf).SelectMany(page => page.Stages))
            {
                var stageId = stage.SharedViewModel.stageId;
                stage.SharedViewModel.Selected.Value = stageId == value;
                stage.SharedViewModel.HasNotification.Value =
                    stageId == stageIdToNotify && !stage.SharedViewModel.Selected.Value;
            }
        }

        private void SubscribeOnClick(WorldMapStage stage)
        {
            SetSelectedStageId(stage.SharedViewModel.stageId, Widget.Find<WorldMap>().StageIdToNotify);
        }

        private void ToggleOn(int pageNumber)
        {
            if(toggles.Count < pageNumber)
                return;

            var toggle = toggles[pageNumber - 1];
            toggle.isOn = false;
            toggle.isOn = true;
        }
    }
}
