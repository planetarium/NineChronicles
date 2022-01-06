using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Quest;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class StageInformation : Widget
    {
        public enum StageType
        {
            None,
            Quest,
            Mimisbrunnr,
        }

        [SerializeField]
        private HelpButton stageHelpButton;

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
        private TextMeshProUGUI closeButtonText;

        [SerializeField]
        private ConditionalButton submitButton;

        [SerializeField]
        private WorldMapWorld world;

        [SerializeField]
        private GameObject buttonNotification;

        [SerializeField]
        private Button closeButton;

        private WorldMap.ViewModel _sharedViewModel;
        private StageType _stageType = StageType.None;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(OnClickClose);
            CloseWidget = OnClickClose;
        }

        public override void Initialize()
        {
            base.Initialize();
            monstersAreaText.text = L10nManager.Localize("UI_WORLD_MAP_MONSTERS");
            rewardsAreaText.text = L10nManager.Localize("UI_REWARDS");
            submitButton.Text = L10nManager.Localize("UI_WORLD_MAP_ENTER");

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

            submitButton.OnSubmitSubject
                .Subscribe(_ => GoToPreparation())
                .AddTo(gameObject);
        }

        private void OnClickClose()
        {
            if (_stageType == StageType.Mimisbrunnr)
            {
                Game.Event.OnRoomEnter.Invoke(true);
            }

            base.Close(true);
        }

        public void Show(WorldMap.ViewModel viewModel, WorldSheet.Row worldRow, StageType stageType)
        {
            _sharedViewModel = viewModel;
            UpdateStageInformation(_sharedViewModel.SelectedStageId.Value, States.Instance.CurrentAvatarState.level);
            _sharedViewModel.WorldInformation.TryGetWorld(worldRow.Id, out var worldModel);
            _sharedViewModel.SelectedStageId
                .Subscribe(stageId => UpdateStageInformation(
                    stageId,
                    States.Instance.CurrentAvatarState?.level ?? 1)
                )
                .AddTo(gameObject);

            closeButtonText.text = L10nManager.Localize($"WORLD_NAME_{worldModel.Name.ToUpper()}");

            if (_sharedViewModel.SelectedStageId.Value == 1)
            {
                stageHelpButton.Show();
            }
            else
            {
                stageHelpButton.Hide();
            }

            _stageType = stageType;

            world.Set(worldRow);
            var questStageId = Game.Game.instance.States
                .CurrentAvatarState.questList
                .OfType<WorldQuest>()
                .Where(x => !x.Complete)
                .OrderBy(x => x.Goal)
                .FirstOrDefault()?
                .Goal ?? -1;

            if (worldModel.IsUnlocked)
            {
                var openedStageId = worldModel.GetNextStageIdForPlay();
                if (worldModel.StageEnd < worldRow.StageEnd &&
                    openedStageId == worldModel.StageEnd &&
                    openedStageId == worldModel.StageClearedId)
                {
                    openedStageId += 1;
                }

                UnlockWorld(openedStageId, worldModel.GetNextStageId());
            }
            else
            {
                LockWorld();
            }

            base.Show(true);
            world.ShowByStageId(_sharedViewModel.SelectedStageId.Value, questStageId);
            HelpTooltip.HelpMe(100003, true);
        }

        private void UpdateStageInformation(int stageId, int characterLevel)
        {
            var worldInfo = _sharedViewModel.WorldInformation;
            var isSubmittable = false;
            if (!(worldInfo is null))
            {
                if (worldInfo.TryGetWorldByStageId(stageId, out var innerWorld))
                {
                    isSubmittable = innerWorld.IsPlayable(stageId);
                }
                else
                {
                    // NOTE: Consider expanding the world.
                    if (Game.Game.instance.TableSheets.WorldSheet.TryGetByStageId(stageId, out var worldRow))
                    {
                        worldInfo.UpdateWorld(worldRow);
                        if (worldInfo.TryGetWorldByStageId(stageId, out var world2))
                        {
                            isSubmittable = world2.IsPlayable(stageId);
                        }
                        else
                        {
                            throw new ArgumentException(nameof(stageId));
                        }
                    }
                    else
                    {
                        throw new ArgumentException(nameof(stageId));
                    }
                }
            }

            submitButton.Interactable = isSubmittable;

            var stageWaveSheet = Game.Game.instance.TableSheets.StageWaveSheet;
            stageWaveSheet.TryGetValue(stageId, out var stageWaveRow, true);
            titleText.text = $"Stage {GetStageIdString(stageWaveRow.StageId, true)}";

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

            buttonNotification.SetActive(stageId == Find<WorldMap>().StageIdToNotify);
        }

        private void GoToPreparation()
        {
            switch (_stageType)
            {
                case StageType.Quest:
                    Find<QuestPreparation>().Show(
                        $"{closeButtonText.text} {_sharedViewModel.SelectedStageId.Value}",
                        true);
                    break;

                case StageType.Mimisbrunnr:
                    var stageId = _sharedViewModel.SelectedStageId.Value;
                    Find<MimisbrunnrPreparation>().Show(
                        $"{closeButtonText.text} {stageId % 10000000}",
                        stageId,
                        true);
                    break;
            }
        }

        private void LockWorld()
        {
            world.Set(-1, world.SharedViewModel.RowData.StageBegin);
        }

        private void UnlockWorld(int openedStageId = -1, int selectedStageId = -1)
        {
            world.Set(openedStageId, selectedStageId);
        }

        public static string GetStageIdString(int stageId, bool isTitle = false)
        {
            var enter = isTitle ? string.Empty : "\n";
            return stageId > 10000000 ? $"<sprite name=icon_Element_1>{enter}{stageId % 10000000}" : stageId.ToString();
        }
    }
}
