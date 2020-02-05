using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.TableData;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Module
{
    public class WorldMapWorld : MonoBehaviour
    {
        public class ViewModel : IDisposable
        {
            public readonly WorldSheet.Row RowData;
            public readonly string WorldName;
            public readonly ReactiveProperty<int> StageIdToShow = new ReactiveProperty<int>(0);
            public readonly ReactiveProperty<int> PageCount = new ReactiveProperty<int>(0);
            public readonly ReactiveProperty<int> CurrentPageNumber = new ReactiveProperty<int>(0);

            public ViewModel(WorldSheet.Row rowData)
            {
                RowData = rowData;
                WorldName = RowData.GetLocalizedName();
            }

            public void Dispose()
            {
                CurrentPageNumber.Dispose();
            }
        }

        public GameObject worldButton;
        public string worldName;
        public TextMeshProUGUI stageNameText;
        public TextMeshProUGUI stagePageText;
        public HorizontalScrollSnap horizontalScrollSnap;
        public List<WorldMapPage> pages;
        public Button previousButton;
        public Button nextButton;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

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
                SharedViewModel.CurrentPageNumber.Value = value + 1);

            foreach (var stage in pages.SelectMany(page => page.stages))
            {
                stage.onClick.Subscribe(SubscribeOnClick)
                    .AddTo(gameObject);
            }
        }

        public void Set(WorldSheet.Row worldRow)
        {
            if (worldRow is null)
                throw new ArgumentNullException(nameof(worldRow));

            _disposablesForModel.DisposeAllAndClear();
            SharedViewModel = new ViewModel(worldRow);

            var stageRows = Game.Game.instance.TableSheets.StageWaveSheet.Values
                .Where(stageRow => stageRow.StageId >= worldRow.StageBegin && stageRow.StageId <= worldRow.StageEnd)
                .ToList();
            var stageRowsCount = stageRows.Count;
            if (worldRow.StagesCount != stageRowsCount)
                throw new SheetRowValidateException(
                    $"{worldRow.Id}: worldRow.StagesCount({worldRow.StagesCount}) != stageRowsCount({stageRowsCount})");

            var stageOffset = 0;
            var nextPageShouldHide = false;
            var shouldDestroyPages = new List<WorldMapPage>();
            foreach (var page in pages)
            {
                if (nextPageShouldHide)
                {
                    shouldDestroyPages.Add(page);

                    continue;
                }

                var stageCount = page.stages.Count;
                var stageModels = new List<WorldMapStage.ViewModel>();
                for (var i = 0; i < stageCount; i++)
                {
                    if (nextPageShouldHide)
                    {
                        stageModels.Add(new WorldMapStage.ViewModel(WorldMapStage.State.Hidden));

                        continue;
                    }

                    var stageRowsIndex = stageOffset + i;
                    if (stageRowsIndex >= stageRowsCount)
                    {
                        nextPageShouldHide = true;
                        stageModels.Add(new WorldMapStage.ViewModel(WorldMapStage.State.Hidden));

                        continue;
                    }

                    var stageModel = new WorldMapStage.ViewModel(
                        stageRows[stageRowsIndex],
                        $"{stageRowsIndex + 1}",
                        WorldMapStage.State.Normal);

                    stageModels.Add(stageModel);
                }

                page.Show(stageModels);
                stageOffset += stageModels.Count;
            }

            foreach (var shouldDestroyPage in shouldDestroyPages)
            {
                pages.Remove(shouldDestroyPage);
                Destroy(shouldDestroyPage.gameObject);
            }

            SharedViewModel.StageIdToShow.Value = worldRow.StageBegin + stageRowsCount - 1;
            SharedViewModel.PageCount.Value = pages.Count;
            SharedViewModel.CurrentPageNumber.Value = 1;

            SharedViewModel.PageCount
                .Subscribe(pageCount => stagePageText.text = $"{SharedViewModel.CurrentPageNumber.Value}/{pageCount}")
                .AddTo(_disposablesForModel);
            SharedViewModel.CurrentPageNumber
                .Subscribe(currentPageNumber =>
                    stagePageText.text = $"{currentPageNumber}/{SharedViewModel.PageCount.Value}")
                .AddTo(_disposablesForModel);

            stageNameText.text = SharedViewModel.WorldName;

            horizontalScrollSnap.ChangePage(SharedViewModel.CurrentPageNumber.Value - 1);
        }

        public void Set(int openedStageId = -1, int selectedStageId = -1)
        {
            foreach (var stage in pages.SelectMany(page => page.stages))
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

                stage.SharedViewModel.state.Value = stageState;
                stage.SharedViewModel.selected.Value = stageId == selectedStageId;
            }
        }

        public void ShowByStageId(int value)
        {
            ShowByPageNumber(GetPageNumber(value));
            SetSelectedStageId(value);

            gameObject.SetActive(true);
        }

        public void ShowByPageNumber(int value)
        {
            SharedViewModel.CurrentPageNumber.Value = value;

            if (horizontalScrollSnap.CurrentPage != SharedViewModel.CurrentPageNumber.Value)
            {
                horizontalScrollSnap.ChangePage(SharedViewModel.CurrentPageNumber.Value - 1);
            }

            horizontalScrollSnap.StartingScreen = SharedViewModel.CurrentPageNumber.Value - 1;

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private int GetPageNumber(int stageId)
        {
            var pageNumber = 1;
            var stageOffset = SharedViewModel.RowData.StageBegin - 1;
            foreach (var page in pages)
            {
                stageOffset += page.stages.Count;
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

        private void SetSelectedStageId(int value)
        {
            foreach (var stage in pages.SelectMany(page => page.stages))
            {
                stage.SharedViewModel.selected.Value = stage.SharedViewModel.stageId == value;
            }
        }

        private void SubscribeOnClick(WorldMapStage stage)
        {
            SetSelectedStageId(stage.SharedViewModel.stageId);
        }
    }
}
