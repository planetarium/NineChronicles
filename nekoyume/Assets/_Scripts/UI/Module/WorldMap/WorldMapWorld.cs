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

        [SerializeField]
        private TextMeshProUGUI stagePageText = null;

        [SerializeField]
        private HorizontalScrollSnap horizontalScrollSnap = null;

        [SerializeField]
        private List<WorldMapPage> pages = null;

        [SerializeField]
        private Button previousButton = null;

        [SerializeField]
        private Button nextButton = null;

        [SerializeField]
        private Image backgroundImage = null;

        [SerializeField]
        private Image backgroundImage2 = null;

        [SerializeField]
        private Image titleImage = null;

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
                SharedViewModel.CurrentPageNumber.Value = value + 1);

            foreach (var stage in pages.SelectMany(page => page.Stages))
            {
                stage.onClick.Subscribe(SubscribeOnClick)
                    .AddTo(gameObject);
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

            var imageKey = worldRow.Id == 101 ? "99" : $"0{worldRow.Id}";
            backgroundImage.sprite = Resources.Load<Sprite>($"UI/Textures/WorldMap/battle_UI_BG_{imageKey}");
            backgroundImage2.sprite = Resources.Load<Sprite>($"UI/Textures/WorldMap/battle_UI_BG_{imageKey}");
            titleImage.sprite = Resources.Load<Sprite>($"UI/Textures/WorldMap/UI_bg_worldmap_{imageKey}");
            titleImage.SetNativeSize();
            var stageOffset = 0;
            var nextPageShouldHide = false;
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

                page.Show(stageModels, imageKey);
                stageOffset += stageModels.Count;
                if (stageOffset >= stageRowsCount)
                {
                    nextPageShouldHide = true;
                }
            }

            SharedViewModel.StageIdToShow.Value = worldRow.StageBegin + stageRowsCount - 1;
            SharedViewModel.PageCount.Value = pages.Count(p => p.gameObject.activeSelf);
            SharedViewModel.CurrentPageNumber.Value = 1;

            SharedViewModel.PageCount
                .Subscribe(pageCount =>
                    stagePageText.text = $"{SharedViewModel.CurrentPageNumber.Value}/{pageCount}")
                .AddTo(_disposablesForModel);
            SharedViewModel.CurrentPageNumber
                .Subscribe(currentPageNumber =>
                {
                    stagePageText.text = $"{currentPageNumber}/{SharedViewModel.PageCount.Value}";
                    previousButton.gameObject.SetActive(currentPageNumber != 1);
                    nextButton.gameObject.SetActive(
                        currentPageNumber != SharedViewModel.PageCount.Value);
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
            ShowByPageNumber(GetPageNumber(value));
            SetSelectedStageId(value, stageIdToNotify);

            gameObject.SetActive(true);
        }

        private void ShowByPageNumber(int value)
        {
            SharedViewModel.CurrentPageNumber.SetValueAndForceNotify(value);
            horizontalScrollSnap.ChangePage(SharedViewModel.CurrentPageNumber.Value - 1);
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
    }
}
