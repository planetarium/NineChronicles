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
            public readonly ReactiveProperty<bool> isWorldShown = new ReactiveProperty<bool>(false);
            public readonly ReactiveProperty<int> selectedStageId = new ReactiveProperty<int>(1);
        }

        public GameObject worldMapRoot;
        public Button alfheimButton;
        public Button svartalfaheimrButton;
        public Button asgardButton;
        public List<WorldMapWorld> worlds = new List<WorldMapWorld>();
        public StageInformation stageInformation;
        public Button submitButton;
        public TextMeshProUGUI submitText;
        public BottomMenu bottomMenu;

        public ViewModel SharedViewModel { get; private set; }

        public int SelectedStageId
        {
            get => SharedViewModel.selectedStageId.Value;
            private set => SharedViewModel.selectedStageId.Value = value;
        }

        #region Mono

        public override void Initialize()
        {
            base.Initialize();
            var firstStageId = Game.Game.instance.TableSheets.StageSheet.First.Id;
            SharedViewModel = new ViewModel();
            SharedViewModel.selectedStageId.Value = firstStageId;
            SharedViewModel.isWorldShown.Subscribe(worldMapRoot.SetActive)
                .AddTo(gameObject);
            SharedViewModel.selectedStageId.Subscribe(UpdateStageInformation)
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
                            SharedViewModel.selectedStageId.Value = worldMapStage.SharedViewModel.stageId)
                        .AddTo(gameObject);
                }
            }

            stageInformation.monstersArea.text.text = LocalizationManager.Localize("UI_WORLD_MAP_MONSTERS");
            stageInformation.rewardsArea.text.text = LocalizationManager.Localize("UI_WORLD_MAP_REWARDS");
            submitText.text = LocalizationManager.Localize("UI_WORLD_MAP_PREPARE");
            bottomMenu.WorldMapButton.text.text = LocalizationManager.Localize("UI_WORLD_MAP");
            bottomMenu.stageButton.text.text = LocalizationManager.Localize("UI_STAGE");

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
            bottomMenu.goToMainButton.button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    GoToMain();
                }).AddTo(gameObject);
            bottomMenu.WorldMapButton.button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    SharedViewModel.isWorldShown.Value = true;
                }).AddTo(gameObject);
            bottomMenu.stageButton.button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    SharedViewModel.isWorldShown.Value = false;
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

        public void ShowByStageId(int stageId)
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
        }

        private void ChangeWorld(string value)
        {
            SharedViewModel.isWorldShown.Value = false;
            
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
            stageInformation.descriptionText.text = stageRow.GetLocalizedDescription();

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
                    image.sprite = SpriteHelper.GetItemIcon(rewardItemRows[i].id);

                    continue;
                }

                image.transform.parent.gameObject.SetActive(false);
            }

            stageInformation.expText.text = $"EXP +{stageRow.TotalExp}";
        }

        private void GoToMain()
        {
            Find<Menu>().ShowRoom();
            Close();
        }
        
        private void GoToQuestPreparation()
        {
            Close();
            Find<QuestPreparation>().Show();
        }
    }
}
