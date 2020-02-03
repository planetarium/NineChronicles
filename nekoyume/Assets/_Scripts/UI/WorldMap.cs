using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
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
            public TextMeshProUGUI titleText;
            public TextMeshProUGUI descriptionText;
            public TextMeshProUGUI monstersAreaText;
            public List<VanillaCharacterView> monstersAreaCharacterViews;
            public TextMeshProUGUI rewardsAreaText;
            public List<StageRewardItemView> rewardsAreaItemViews;
            public TextMeshProUGUI expText;
        }

        public class ViewModel
        {
            public readonly ReactiveProperty<bool> IsWorldShown = new ReactiveProperty<bool>(false);
            public readonly ReactiveProperty<int> SelectedWorldId = new ReactiveProperty<int>(1);
            public readonly ReactiveProperty<int> SelectedStageId = new ReactiveProperty<int>(1);

            public WorldInformation WorldInformation;
        }

        public List<WorldMapWorld> worlds = new List<WorldMapWorld>();

        public GameObject worldMapRoot;
        public Button alfheimButton;
        public Button svartalfheimButton;
        public Button asgardButton;
        public Button challengeModeButton;

        public StageInformation stageInformation;
        public SubmitButton submitButton;

        private readonly List<IDisposable> _disposablesAtShow = new List<IDisposable>();

        public ViewModel SharedViewModel { get; private set; }

        public int SelectedWorldId
        {
            get => SharedViewModel.SelectedWorldId.Value;
            private set => SharedViewModel.SelectedWorldId.SetValueAndForceNotify(value);
        }

        public int SelectedStageId
        {
            get => SharedViewModel.SelectedStageId.Value;
            private set => SharedViewModel.SelectedStageId.SetValueAndForceNotify(value);
        }

        private int SelectedWorldStageBegin { get; set; }

        #region Mono

        public override void Initialize()
        {
            base.Initialize();
            var firstStageId = Game.Game.instance.TableSheets.StageWaveSheet.First?.Id ?? 1;
            SharedViewModel = new ViewModel();
            SharedViewModel.SelectedStageId.Value = firstStageId;
            // �ʱ� �� ���� 1ȸ ����
            SharedViewModel.IsWorldShown.Skip(1).Subscribe(UpdateWorld).AddTo(gameObject);
            SharedViewModel.SelectedStageId.Subscribe(UpdateStageInformation).AddTo(gameObject);

            var tooltip = Widget.Find<ItemInformationTooltip>();

            foreach(var view in stageInformation.rewardsAreaItemViews)
            {
                view.touchHandler.OnClick.Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    var model = new Model.CountableItem(new Nekoyume.Model.Item.Material(view.Data as MaterialItemSheet.Row), 1);
                    tooltip.Show(view.RectTransform, model);
                    tooltip.itemInformation.iconArea.itemView.countText.enabled = false;
                }).AddTo(view);
            }

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
                        {
                            SharedViewModel.SelectedStageId.Value = worldMapStage.SharedViewModel.stageId;
                        })
                        .AddTo(gameObject);
                }
            }

            stageInformation.monstersAreaText.text = LocalizationManager.Localize("UI_WORLD_MAP_MONSTERS");
            stageInformation.rewardsAreaText.text = LocalizationManager.Localize("UI_REWARDS");
            submitButton.SetSubmitText(LocalizationManager.Localize("UI_WORLD_MAP_ENTER"));

            alfheimButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    ShowWorld(1);
                }).AddTo(gameObject);
            svartalfheimButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    ShowWorld(2);
                }).AddTo(gameObject);
            asgardButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    ShowWorld(3);
                }).AddTo(gameObject);
            challengeModeButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    ShowWorld(101);
                }).AddTo(gameObject);
            submitButton.OnSubmitClick
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    GoToQuestPreparation();
                }).AddTo(gameObject);
        }

        #endregion

        public void Show(WorldInformation worldInformation)
        {
            SharedViewModel.WorldInformation = worldInformation;
            if (worldInformation is null)
            {
                foreach (var world in worlds)
                {
                    LockWorld(world);
                }

                return;
            }

            foreach (var world in worlds)
            {
                var worldId = world.SharedViewModel.RowData.Id;
                if (!worldInformation.TryGetWorld(worldId, out var worldModel))
                    throw new Exception(nameof(worldId));

                if (worldModel.IsUnlocked)
                {
                    UnlockWorld(world,
                        worldModel.GetNextStageIdForPlay(),
                        worldModel.GetNextStageId());
                }
                else
                {
                    LockWorld(world);
                }
            }

            if (!worldInformation.TryGetFirstWorld(out var firstWorld))
                throw new Exception("worldInformation.TryGetFirstWorld() failed!");

            Show(firstWorld.Id, firstWorld.GetNextStageId(), true);
        }

        public void Show(int worldId, int stageId, bool showWorld)
        {
            var bottomMenu = Find<BottomMenu>();
            bottomMenu.Show(
                UINavigator.NavigationType.Back,
                SubscribeBackButtonClick,
                true,
                BottomMenu.ToggleableType.WorldMap);
            bottomMenu.worldMapButton.button.OnClickAsObservable()
                .Subscribe(_ => SharedViewModel.IsWorldShown.SetValueAndForceNotify(true))
                .AddTo(_disposablesAtShow);

            ShowWorld(worldId, stageId, showWorld);
            Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesAtShow.DisposeAllAndClear();
            Find<BottomMenu>().Close(ignoreCloseAnimation);
            base.Close(ignoreCloseAnimation);
        }

        private void LockWorld(WorldMapWorld world)
        {
            world.Set(-1, world.SharedViewModel.RowData.StageBegin);
            world.worldButton.SetActive(false);
        }

        private void UnlockWorld(WorldMapWorld world, int openedStageId = -1, int selectedStageId = -1)
        {
            world.Set(openedStageId, selectedStageId);
            world.worldButton.SetActive(true);
        }

        private void ShowWorld(int worldId)
        {
            if (!SharedViewModel.WorldInformation.TryGetWorld(worldId, out var world))
                throw new ArgumentException(nameof(worldId));

            ShowWorld(world.Id, world.GetNextStageId(), false);
        }

        private void ShowWorld(int worldId, int stageId, bool showWorld)
        {
            SharedViewModel.IsWorldShown.SetValueAndForceNotify(showWorld);
            SelectedWorldId = worldId;
            Game.Game.instance.TableSheets.WorldSheet.TryGetValue(SelectedWorldId, out var worldRow, true);
            SelectedWorldStageBegin = worldRow.StageBegin;
            SelectedStageId = stageId;

            foreach (var world in worlds)
            {
                if (world.SharedViewModel.RowData.Id.Equals(SelectedWorldId))
                {
                    world.ShowByStageId(SelectedStageId);
                }
                else
                {
                    world.Hide();
                }
            }
        }

        private void UpdateWorld(bool active)
        {
            var status = Find<Status>();

            if (active)
            {
                var bottomMenu = Find<BottomMenu>();
                bottomMenu.worldMapButton.Hide();
                bottomMenu.backButton.Show();
                worldMapRoot.SetActive(true);
                status.Close();
            }
            else
            {
                worldMapRoot.SetActive(false);
                var bottomMenu = Find<BottomMenu>();
                bottomMenu.worldMapButton.Show();
                bottomMenu.backButton.Hide();
                bottomMenu.ToggleGroup?.SetToggledOffAll();
                status.Show();
            }
        }

        private void UpdateStageInformation(int stageId)
        {
            var isSubmittable = false;
            if (!(SharedViewModel.WorldInformation is null))
            {
                if (!SharedViewModel.WorldInformation.TryGetWorldByStageId(stageId, out var world))
                    throw new ArgumentException(nameof(stageId));

                isSubmittable = world.IsPlayable(stageId);
            }

            var stageSheet = Game.Game.instance.TableSheets.StageWaveSheet;
            stageSheet.TryGetValue(stageId, out var stageRow, true);
            stageInformation.titleText.text = $"Stage #{stageRow.Id - SelectedWorldStageBegin + 1}";

            var monsterCount = stageRow.TotalMonsterIds.Count;
            for (var i = 0; i < stageInformation.monstersAreaCharacterViews.Count; i++)
            {
                var characterView = stageInformation.monstersAreaCharacterViews[i];
                if (i < monsterCount)
                {
                    characterView.Show();
                    characterView.SetData(stageRow.TotalMonsterIds[i]);

                    continue;
                }

                characterView.Hide();
            }

            var rewardItemRows = stageRow.GetRewardItemRows();
            var rewardItemCount = rewardItemRows.Count;
            for (var i = 0; i < stageInformation.rewardsAreaItemViews.Count; i++)
            {
                var itemView = stageInformation.rewardsAreaItemViews[i];
                if (i < rewardItemCount)
                {
                    itemView.Show();
                    itemView.SetData(rewardItemRows[i]);

                    continue;
                }

                itemView.Hide();
            }

            stageInformation.expText.text = $"EXP +{stageRow.TotalExp}";

            submitButton.SetSubmittable(isSubmittable);
        }

        private void SubscribeBackButtonClick(BottomMenu bottomMenu)
        {
            SharedViewModel.IsWorldShown.SetValueAndForceNotify(false);
            Close();
            Game.Event.OnRoomEnter.Invoke();
        }

        private void GoToQuestPreparation()
        {
            Close();
            Find<Status>().Close();
            Find<QuestPreparation>().ToggleWorldMap();
        }
    }
}
