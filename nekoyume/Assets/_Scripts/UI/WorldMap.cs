using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Model;
using Nekoyume.State;
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
        public WorldButton yggdrasilButton;
        public WorldButton alfheimButton;
        public WorldButton svartalfheimButton;
        public WorldButton asgardButton;
        public WorldButton hardModeButton;

        public GameObject stage;
        public StageInformation stageInformation;
        public SubmitButton submitButton;
        public Animator worldMapAnimator;

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

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = null;
        }

        public override void Initialize()
        {
            base.Initialize();
            var firstStageId = Game.Game.instance.TableSheets.StageWaveSheet.First?.StageId ?? 1;
            SharedViewModel = new ViewModel();
            SharedViewModel.SelectedStageId.Value = firstStageId;
            SharedViewModel.IsWorldShown.Skip(1).Subscribe(UpdateWorld).AddTo(gameObject);
            SharedViewModel.SelectedStageId.Subscribe(stageId =>
            {
                UpdateStageInformation(
                    stageId,
                    States.Instance.CurrentAvatarState?.level ?? 1);
            }).AddTo(gameObject);

            var tooltip = Find<ItemInformationTooltip>();

            foreach (var view in stageInformation.rewardsAreaItemViews)
            {
                view.touchHandler.OnClick.Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    var model = new Model.CountableItem(
                        new Nekoyume.Model.Item.Material(view.Data as MaterialItemSheet.Row),
                        1);
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
                        SharedViewModel.SelectedStageId.Value =
                            worldMapStage.SharedViewModel.stageId;
                    }).AddTo(gameObject);
                }
            }

            stageInformation.monstersAreaText.text = LocalizationManager.Localize("UI_WORLD_MAP_MONSTERS");
            stageInformation.rewardsAreaText.text = LocalizationManager.Localize("UI_REWARDS");
            submitButton.SetSubmitText(LocalizationManager.Localize("UI_WORLD_MAP_ENTER"));

            yggdrasilButton.OnClickSubject
                .Subscribe(_ => ShowWorld(1))
                .AddTo(gameObject);
            alfheimButton.OnClickSubject
                .Subscribe(_ => ShowWorld(2))
                .AddTo(gameObject);
            svartalfheimButton.OnClickSubject
                .Subscribe(_ => ShowWorld(3))
                .AddTo(gameObject);
            asgardButton.OnClickSubject
                .Subscribe(_ => ShowWorld(4))
                .AddTo(gameObject);
            hardModeButton.OnClickSubject
                .Subscribe(_ => ShowWorld(101))
                .AddTo(gameObject);
            submitButton.OnSubmitClick
                .Subscribe(_ => GoToQuestPreparation())
                .AddTo(gameObject);
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
                    UnlockWorld(
                        world,
                        worldModel.GetNextStageIdForPlay(),
                        worldModel.GetNextStageId());
                }
                else
                {
                    LockWorld(world);
                }
            }

            if (!worldInformation.TryGetFirstWorld(out var firstWorld))
            {
                throw new Exception("worldInformation.TryGetFirstWorld() failed!");
            }

            Show(firstWorld.Id, firstWorld.GetNextStageId(), true, true);
        }

        public void Show(int worldId, int stageId, bool showWorld, bool callByShow = false)
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

            ShowWorld(worldId, stageId, showWorld, callByShow);
            Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesAtShow.DisposeAllAndClear();
            Find<BottomMenu>().Close(true);
            stage.SetActive(false);
            base.Close(ignoreCloseAnimation);
        }

        private static void LockWorld(WorldMapWorld world)
        {
            world.Set(-1, world.SharedViewModel.RowData.StageBegin);
            world.worldButton.Lock();
        }

        private static void UnlockWorld(WorldMapWorld world, int openedStageId = -1, int selectedStageId = -1)
        {
            world.Set(openedStageId, selectedStageId);
            world.worldButton.Unlock();
        }

        private void ShowWorld(int worldId)
        {
            if (!SharedViewModel.WorldInformation.TryGetWorld(worldId, out var world))
                throw new ArgumentException(nameof(worldId));

            CloseWidget = Find<BottomMenu>().worldMapButton.button.onClick.Invoke;
            CloseWidget += Pop;
            CloseWidget += () => CloseWidget = null;
            Push();

            ShowWorld(world.Id, world.GetNextStageId(), false);
        }

        private void ShowWorld(int worldId, int stageId, bool showWorld, bool callByShow = false)
        {
            if (callByShow)
            {
                CallByShowUpdateWorld();
            }
            else
            {
                SharedViewModel.IsWorldShown.SetValueAndForceNotify(showWorld);
            }

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

        private void CallByShowUpdateWorld()
        {
            var status = Find<Status>();

            var bottomMenu = Find<BottomMenu>();
            bottomMenu.worldMapButton.Hide();
            bottomMenu.backButton.Show();
            stage.SetActive(false);
            status.Close(true);
            worldMapRoot.SetActive(true);
        }

        private void UpdateWorld(bool active)
        {
            var status = Find<Status>();

            if (active)
            {
                var bottomMenu = Find<BottomMenu>();
                bottomMenu.worldMapButton.Hide();
                bottomMenu.backButton.Show();
                status.Close(true);
                worldMapRoot.SetActive(true);
            }
            else
            {
                var bottomMenu = Find<BottomMenu>();
                bottomMenu.Show(UINavigator.NavigationType.Back, SubscribeBackButtonClick, true,
                    BottomMenu.ToggleableType.WorldMap);
                bottomMenu.worldMapButton.Show();
                bottomMenu.backButton.Hide();
                bottomMenu.ToggleGroup?.SetToggledOffAll();
                status.Show();
                worldMapRoot.SetActive(false);
                stage.SetActive(false);
                stage.SetActive(true);
            }
        }

        private void UpdateStageInformation(int stageId, int characterLevel)
        {
            var isSubmittable = false;
            if (!(SharedViewModel.WorldInformation is null))
            {
                if (!SharedViewModel.WorldInformation.TryGetWorldByStageId(stageId, out var world))
                    throw new ArgumentException(nameof(stageId));

                isSubmittable = world.IsPlayable(stageId);
            }

            var stageWaveSheet = Game.Game.instance.TableSheets.StageWaveSheet;
            stageWaveSheet.TryGetValue(stageId, out var stageWaveRow, true);
            stageInformation.titleText.text = $"Stage #{stageWaveRow.StageId - SelectedWorldStageBegin + 1}";

            var monsterCount = stageWaveRow.TotalMonsterIds.Count;
            for (var i = 0; i < stageInformation.monstersAreaCharacterViews.Count; i++)
            {
                var characterView = stageInformation.monstersAreaCharacterViews[i];
                if (i < monsterCount)
                {
                    characterView.Show();
                    characterView.SetData(stageWaveRow.TotalMonsterIds[i]);

                    continue;
                }

                characterView.Hide();
            }

            var stageSheet = Game.Game.instance.TableSheets.StageSheet;
            stageSheet.TryGetValue(stageId, out var stageRow, true);
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

            var exp = StageRewardExpHelper.GetExp(characterLevel, stageId);
            stageInformation.expText.text = $"EXP +{exp}";

            submitButton.SetSubmittable(isSubmittable);
        }

        private void SubscribeBackButtonClick(BottomMenu bottomMenu)
        {
            SharedViewModel.IsWorldShown.SetValueAndForceNotify(false);
            Close();
            Game.Event.OnRoomEnter.Invoke(true);
        }

        private void GoToQuestPreparation()
        {
            Close();
            Find<Status>().Close(true);
            Find<QuestPreparation>().Show();
        }
    }
}
