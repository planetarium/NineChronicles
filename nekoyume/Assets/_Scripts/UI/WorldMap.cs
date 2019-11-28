using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class WorldMap : Widget
    {
        [Serializable]
        public struct StageInformation
        {
            [Serializable]
            public struct IconsArea
            {
                public RectTransform root;
                public TextMeshProUGUI text;
                public List<Image> iconImages;
            }

            public TextMeshProUGUI titleText;
            public TextMeshProUGUI descriptionText;
            public IconsArea monstersArea;
            public IconsArea rewardsArea;
            public TextMeshProUGUI expText;
        }

        public class ViewModel
        {
            public readonly ReactiveProperty<bool> IsWorldShown = new ReactiveProperty<bool>(false);
            public readonly ReactiveProperty<int> SelectedStageId = new ReactiveProperty<int>(1);
        }

        public List<WorldMapWorld> worlds = new List<WorldMapWorld>();

        public GameObject worldMapRoot;
        public Button alfheimButton;
        public Button svartalfaheimrButton;
        public Button asgardButton;

        public StageInformation stageInformation;
        public Button submitButton;
        public TextMeshProUGUI submitText;
        
        private readonly List<IDisposable> _disposablesAtShow = new List<IDisposable>();

        public ViewModel SharedViewModel { get; private set; }

        public int SelectedStageId
        {
            get => SharedViewModel.SelectedStageId.Value;
            private set => SharedViewModel.SelectedStageId.Value = value;
        }

        #region Mono

        public override void Initialize()
        {
            base.Initialize();
            var firstStageId = Game.Game.instance.TableSheets.StageSheet.First?.Id ?? 1;
            SharedViewModel = new ViewModel();
            SharedViewModel.SelectedStageId.Value = firstStageId;
            SharedViewModel.IsWorldShown.Subscribe(isWorldShown =>
                {
                    if (isWorldShown)
                    {
                        Find<BottomMenu>().worldMapButton.Hide();
                        worldMapRoot.SetActive(true);
                    }
                    else
                    {
                        worldMapRoot.SetActive(false);
                        Find<BottomMenu>().worldMapButton.Show();
                    }
                })
                .AddTo(gameObject);
            SharedViewModel.SelectedStageId.Subscribe(UpdateStageInformation)
                .AddTo(gameObject);

            var sheet = Game.Game.instance.TableSheets.WorldSheet;
            foreach (var world in worlds)
            {
                if (!sheet.TryGetByName(world.worldName, out var row))
                {
                    throw new SheetRowNotFoundException("WorldSheet", "Name", world.worldName);
                }

                world.Set(row);

                foreach (var stage in world.pages.SelectMany(page => page.stages))
                {
                    stage.onClick.Subscribe(worldMapStage =>
                            SharedViewModel.SelectedStageId.Value = worldMapStage.SharedViewModel.stageId)
                        .AddTo(gameObject);
                }
            }

            stageInformation.monstersArea.text.text = LocalizationManager.Localize("UI_WORLD_MAP_MONSTERS");
            stageInformation.rewardsArea.text.text = LocalizationManager.Localize("UI_WORLD_MAP_REWARDS");
            submitText.text = LocalizationManager.Localize("UI_WORLD_MAP_ENTER");

            alfheimButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    ChangeWorld("Alfheim");
                }).AddTo(gameObject);
            svartalfaheimrButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    ChangeWorld("Svartalfaheimr");
                }).AddTo(gameObject);
            asgardButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    ChangeWorld("Asgard");
                }).AddTo(gameObject);
            submitButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    GoToQuestPreparation();
                }).AddTo(gameObject);

            ReactiveCurrentAvatarState.WorldStage.Subscribe(clearedStageId =>
            {
                foreach (var world in worlds)
                {
                    world.Set(clearedStageId, SelectedStageId);
                }
            }).AddTo(gameObject);
        }

        #endregion

        public void Show(int stageId)
        {
            SelectedStageId = stageId;
            var tableSheets = Game.Game.instance.TableSheets;
            if (!tableSheets.WorldSheet.TryGetByStageId(SelectedStageId, out var worldRow))
                throw new SheetRowNotFoundException("WorldSheet", "TryGetByStageId()", SelectedStageId.ToString());

            foreach (var world in worlds)
            {
                if (world.worldName.Equals(worldRow.Name))
                {
                    world.ShowByStageId(SelectedStageId);
                }
                else
                {
                    world.Hide();
                }
            }

            Show();

            var bottomMenu = Find<BottomMenu>();
            bottomMenu.Show(
                UINavigator.NavigationType.Back,
                SubscribeBackButtonClick,
                true,
                BottomMenu.ToggleableType.WorldMap);
            bottomMenu.worldMapButton.button.OnClickAsObservable()
                .Subscribe(_ => SharedViewModel.IsWorldShown.Value = true)
                .AddTo(_disposablesAtShow);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesAtShow.DisposeAllAndClear();
            Find<BottomMenu>().Close(ignoreCloseAnimation);
            base.Close(ignoreCloseAnimation);
        }

        private void ChangeWorld(string value)
        {
            SharedViewModel.IsWorldShown.Value = false;
            var bottomMenu = Find<BottomMenu>();
            bottomMenu.worldMapButton.SetToggledOff();

            foreach (var world in worlds)
            {
                if (world.worldName.Equals(value))
                {
                    world.ShowByPageNumber(1);
                }
                else
                {
                    world.Hide();
                }
            }
        }

        private void UpdateStageInformation(int stageId)
        {
            var tableSheets = Game.Game.instance.TableSheets;
            if (!tableSheets.StageSheet.TryGetValue(stageId, out var stageRow))
                throw new SheetRowNotFoundException("StageSheet", SelectedStageId.ToString());

            stageInformation.titleText.text = $"Stage #{SelectedStageId}";

            var monsterCount = stageRow.TotalMonsterIds.Count;
            for (var i = 0; i < stageInformation.monstersArea.iconImages.Count; i++)
            {
                var image = stageInformation.monstersArea.iconImages[i];
                if (i < monsterCount)
                {
                    image.transform.parent.gameObject.SetActive(true);
                    image.sprite = SpriteHelper.GetCharacterIcon(stageRow.TotalMonsterIds[i]);

                    continue;
                }

                image.transform.parent.gameObject.SetActive(false);
            }

            var rewardItemRows = stageRow.GetRewardItemRows();
            for (var i = 0; i < stageInformation.rewardsArea.iconImages.Count; i++)
            {
                var image = stageInformation.rewardsArea.iconImages[i];
                if (i < rewardItemRows.Count)
                {
                    image.transform.parent.gameObject.SetActive(true);
                    image.sprite = SpriteHelper.GetItemIcon(rewardItemRows[i].Id);

                    continue;
                }

                image.transform.parent.gameObject.SetActive(false);
            }

            stageInformation.expText.text = $"EXP +{stageRow.TotalExp}";
        }

        private void SubscribeBackButtonClick(BottomMenu bottomMenu)
        {
            if (SharedViewModel.IsWorldShown.Value)
            {
                SharedViewModel.IsWorldShown.Value = false;
                bottomMenu.worldMapButton.SetToggledOff();
            }
            else
            {
                Close();
                Find<Menu>().ShowRoom();
            }
        }

        private void GoToQuestPreparation()
        {
            Close();
            Find<QuestPreparation>().ToggleWorldMap();
        }
    }
}
