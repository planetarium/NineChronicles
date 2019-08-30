using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.Game.Controller;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class WorldMap : Widget
    {
        public Module.WorldMapChapter[] chapters;

        public GameObject world;
        public GameObject stage;
        public Transform chapterContainer;
        public Button[] mainButtons;
        public Text[] mainButtonTexts;
        public Button worldButton;
        public Text worldButtonText;
        public Button previousButton;
        public Text pageText;
        public Button nextButton;
        public Button[] closeButtons;

        private WorldSheet.Row _currentWorld;
        private WorldChapterSheet.Row _currentChapter;
        private int _selectedStage = -1;

        private Module.WorldMapChapter _chapter;
        private readonly List<IDisposable> _disposablesForChapter = new List<IDisposable>();

        public int SelectedStage
        {
            get
            {
                if (_selectedStage < 0)
                {
                    _selectedStage = States.Instance.currentAvatarState.Value.worldStage;
                }

                return _selectedStage;
            }
            set => _selectedStage = value;
        }

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            foreach (var mainButtonText in mainButtonTexts)
            {
                mainButtonText.text = LocalizationManager.Localize("UI_MAIN");
            }

            worldButtonText.text = LocalizationManager.Localize("UI_WORLD");

            foreach (var mainButton in mainButtons)
            {
                mainButton.OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        AudioController.PlayClick();
                        GoToMenu();
                    }).AddTo(gameObject);
            }

            worldButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    ShowWorld();
                }).AddTo(gameObject);

            previousButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    LoadWorld(_currentWorld.Id - 1);
                }).AddTo(gameObject);

            nextButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    LoadWorld(_currentWorld.Id + 1);
                }).AddTo(gameObject);

            foreach (var closeButton in closeButtons)
            {
                closeButton.OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        AudioController.PlayClick();
                        GoToMenu();
                    }).AddTo(gameObject);
            }
        }

        #endregion

        public void Show(bool useAvatarState)
        {
            if (useAvatarState)
                SelectedStage = States.Instance.currentAvatarState.Value.worldStage;
            
            ShowChapter();
            Show();
        }

        public void LoadWorld(int worldId)
        {
            if (!Game.Game.instance.TableSheets.WorldSheet.TryGetValue(worldId, out var worldRow))
            {
                throw new KeyNotFoundException($"worldId({worldId})");
            }

            LoadWorld(worldRow, worldRow.ChapterBegin);
        }

        private void LoadWorld(WorldSheet.Row worldRow, int chapterId)
        {
            _currentWorld = worldRow;

            if (chapterId < _currentWorld.ChapterBegin
                || chapterId > _currentWorld.ChapterEnd)
            {
                throw new ArgumentOutOfRangeException($"chapterId({chapterId})");
            }

            ChangeChapter(chapterId);
            SetModelToChapter();

            previousButton.interactable = _currentWorld.Id > 1;
            nextButton.interactable = _currentWorld.Id < Game.Game.instance.TableSheets.WorldSheet.Count;
            pageText.text = $"{_currentWorld.Id} / {Game.Game.instance.TableSheets.WorldSheet.Count}";

            ShowStage();
        }

        private void ChangeChapter(int chapterId)
        {
            if (_chapter)
            {
                _chapter.Model.Dispose();
                _chapter = null;
            }

            if (!Game.Game.instance.TableSheets.WorldChapterSheet.TryGetValue(chapterId, out _currentChapter))
            {
                throw new KeyNotFoundException($"chapterId({chapterId})");
            }

            if (!TryGetWorldMapChapter(_currentChapter.Prefab, out var worldMapChapter))
            {
                throw new FailedToLoadResourceException<WorldMapChapter>();
            }

            if (chapterContainer.childCount > 0)
            {
                Destroy(chapterContainer.GetChild(0).gameObject);
            }

            _chapter = Instantiate(worldMapChapter, chapterContainer);
        }

        private void SetModelToChapter()
        {
            _disposablesForChapter.DisposeAllAndClear();

            var previousStage = 0;
            var stageModels = new List<WorldMapStage>();
            WorldMapStage currentStageModel = null;
            foreach (var stageRow in Game.Game.instance.TableSheets.StageSheet)
            {
                if (stageRow.Stage < _currentChapter.StageBegin
                    || stageRow.Stage > _currentChapter.StageEnd)
                {
                    continue;
                }

                var currentStage = stageRow.Stage;

                if (previousStage != currentStage)
                {
                    var stageState = WorldMapStage.State.Normal;
                    if (stageRow.Stage == SelectedStage)
                    {
                        stageState = WorldMapStage.State.Selected;
                    }
                    else if (stageRow.Stage < States.Instance.currentAvatarState.Value.worldStage)
                    {
                        stageState = WorldMapStage.State.Cleared;
                    }
                    else if (stageRow.Stage > States.Instance.currentAvatarState.Value.worldStage)
                    {
                        stageState = WorldMapStage.State.Disabled;
                    }

                    currentStageModel = new WorldMapStage(stageState, currentStage, false);
                    currentStageModel.onClick.Subscribe(_ =>
                    {
                        SelectedStage = _.Model.stage.Value;
                        GoToQuestPreparation();
                    }).AddTo(_disposablesForChapter);

                    stageModels.Add(currentStageModel);
                }

                if (stageRow.IsBoss
                    && currentStageModel != null)
                {
                    currentStageModel.hasBoss.Value = true;
                }

                previousStage = currentStage;
            }

            var chapterModel = new WorldMapChapter(stageModels);
            _chapter.SetModel(chapterModel);
        }

        private bool TryGetWorldMapChapter(string chapterPrefab, out Module.WorldMapChapter worldMapChapter)
        {
            foreach (var chapter in chapters)
            {
                if (!chapter.name.Equals(chapterPrefab))
                {
                    continue;
                }

                worldMapChapter = chapter;
                return true;
            }

            worldMapChapter = null;
            return false;
        }

        private void ShowWorld()
        {
            world.SetActive(true);
            stage.SetActive(false);
        }

        private void ShowStage()
        {
            world.SetActive(false);
            stage.SetActive(true);
        }

        private void ShowChapter()
        {
            if (!Game.Game.instance.TableSheets.WorldChapterSheet.TryGetByStage(SelectedStage, out var chapter))
            {
                throw new SheetRowNotFoundException();
            }

            foreach (var worldRow in Game.Game.instance.TableSheets.WorldSheet)
            {
                if (chapter.Id < worldRow.ChapterBegin
                    || chapter.Id > worldRow.ChapterEnd)
                {
                    continue;
                }

                LoadWorld(worldRow, chapter.Id);

                break;
            }
        }

        private void GoToMenu()
        {
            Close();
            Find<Menu>().ShowRoom();
        }

        private void GoToQuestPreparation()
        {
            Close();
            Find<QuestPreparation>().Show();
        }
    }
}
