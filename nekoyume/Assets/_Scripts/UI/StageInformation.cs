using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Quest;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI
{
    public class StageInformation : Widget
    {
        [SerializeField]
        private HelpButton stageHelpButton = null;
        [SerializeField]
        private TextMeshProUGUI titleText;
        [SerializeField]
        private TextMeshProUGUI monstersAreaText;
        [SerializeField]
        private List<VanillaCharacterView> monstersAreaCharacterViews;
        [SerializeField]
        private TextMeshProUGUI rewardsAreaText;
        [SerializeField]
        private List<StageRewardItemView> rewardsAreaItemViews;
        [SerializeField]
        private TextMeshProUGUI expText;
        [SerializeField]
        private SubmitButton submitButton = null;
        [SerializeField]
        private WorldMapWorld world = null;
        [SerializeField]
        private GameObject buttonNotification = null;

        private WorldMap.ViewModel _sharedViewModel;


        public override void Initialize()
        {
            base.Initialize();
            monstersAreaText.text = LocalizationManager.Localize("UI_WORLD_MAP_MONSTERS");
            rewardsAreaText.text = LocalizationManager.Localize("UI_REWARDS");
            submitButton.SetSubmitText(LocalizationManager.Localize("UI_WORLD_MAP_ENTER"));

            var tooltip = Find<ItemInformationTooltip>();
            foreach (var view in rewardsAreaItemViews)
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

            foreach (var stage in world.Pages.SelectMany(page => page.Stages))
            {
                stage.onClick.Subscribe(worldMapStage =>
                {
                    _sharedViewModel.SelectedStageId.Value =
                        worldMapStage.SharedViewModel.stageId;
                }).AddTo(gameObject);
            }

            submitButton.OnSubmitClick
                .Subscribe(_ => GoToQuestPreparation())
                .AddTo(gameObject);
        }

        public void Show(WorldMap.ViewModel viewModel, WorldSheet.Row worldRow)
        {
            _sharedViewModel = viewModel;
            _sharedViewModel.SelectedStageId
                .Subscribe(stageId => UpdateStageInformation(
                    stageId,
                    States.Instance.CurrentAvatarState?.level ?? 1)
                )
                .AddTo(gameObject);
            _sharedViewModel.WorldInformation.TryGetWorld(worldRow.Id, out var worldModel);
            UpdateStageInformation(_sharedViewModel.SelectedStageId.Value, States.Instance.CurrentAvatarState.level);
            if (_sharedViewModel.SelectedStageId.Value == 1)
            {
                stageHelpButton.Show();
            }
            else
            {
                stageHelpButton.Hide();
            }
            var bottomMenu = Find<BottomMenu>();
            bottomMenu.Show(
                UINavigator.NavigationType.Back,
                SubscribeBackButtonClick,
                true,
                BottomMenu.ToggleableType.WorldMap);
            bottomMenu.worldMapButton.OnClick
                .Subscribe(_ => BackToWorldMap())
                .AddTo(gameObject);
            bottomMenu.ToggleGroup?.SetToggledOffAll();
            world.Set(worldRow);
            var questStageId = Game.Game.instance.States
                .CurrentAvatarState.questList
                .OfType<WorldQuest>()
                .Where(x => !x.Complete)
                .OrderBy(x => x.Goal)
                .FirstOrDefault()?
                .Goal ?? -1;
            world.ShowByStageId(_sharedViewModel.SelectedStageId.Value, questStageId);
            if (worldModel.IsUnlocked)
            {
                UnlockWorld(worldModel.GetNextStageIdForPlay(), worldModel.GetNextStageId());
            }
            else
            {
                LockWorld();
            }

            base.Show();
        }

        private void SubscribeBackButtonClick(BottomMenu bottomMenu)
        {
            BackToWorldMap();
        }

        private void BackToWorldMap()
        {
            Close();
            Find<WorldMap>().Show(States.Instance.CurrentAvatarState.worldInformation);
        }

        private void UpdateStageInformation(int stageId, int characterLevel)
        {
            var isSubmittable = false;
            if (!(_sharedViewModel.WorldInformation is null))
            {
                if (!_sharedViewModel.WorldInformation.TryGetWorldByStageId(stageId, out var world))
                    throw new ArgumentException(nameof(stageId));

                isSubmittable = world.IsPlayable(stageId);
            }

            var stageWaveSheet = Game.Game.instance.TableSheets.StageWaveSheet;
            stageWaveSheet.TryGetValue(stageId, out var stageWaveRow, true);
            titleText.text = $"Stage {stageWaveRow.StageId}";

            var monsterCount = stageWaveRow.TotalMonsterIds.Count;
            for (var i = 0; i < monstersAreaCharacterViews.Count; i++)
            {
                var characterView = monstersAreaCharacterViews[i];
                if (i < monsterCount)
                {
                    characterView.Show();
                    characterView.SetByCharacterId(stageWaveRow.TotalMonsterIds[i]);

                    continue;
                }

                characterView.Hide();
            }

            var stageSheet = Game.Game.instance.TableSheets.StageSheet;
            stageSheet.TryGetValue(stageId, out var stageRow, true);
            var rewardItemRows = stageRow.GetRewardItemRows();
            var rewardItemCount = rewardItemRows.Count;
            for (var i = 0; i < rewardsAreaItemViews.Count; i++)
            {
                var itemView = rewardsAreaItemViews[i];
                if (i < rewardItemCount)
                {
                    itemView.Show();
                    itemView.SetData(rewardItemRows[i]);

                    continue;
                }

                itemView.Hide();
            }

            var exp = StageRewardExpHelper.GetExp(characterLevel, stageId);
            expText.text = $"EXP +{exp}";

            submitButton.SetSubmittable(isSubmittable);
            buttonNotification.SetActive(stageId == Find<WorldMap>().StageIdToNotify);
        }

        private void GoToQuestPreparation()
        {
            Close();
            Find<WorldMap>().Close(true);
            Find<QuestPreparation>().Show();
        }

        private void LockWorld()
        {
            world.Set(-1, world.SharedViewModel.RowData.StageBegin);
        }

        private void UnlockWorld(int openedStageId = -1, int selectedStageId = -1)
        {
            world.Set(openedStageId, selectedStageId);
        }
    }
}
