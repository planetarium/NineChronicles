using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.EnumType;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Quest;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Event;
using Nekoyume.UI.Module;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class StageInformation : Widget
    {
        [SerializeField]
        private HelpButton stageHelpButton;

        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private List<VanillaCharacterView> monstersAreaCharacterViews;

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

        [SerializeField]
        private GameObject[] seasonPassObjs;

        [SerializeField]
        private TextMeshProUGUI seasonPassCourageAmount;

        private WorldMap.ViewModel _sharedViewModel;
        private StageType _stageType;
        private readonly List<IDisposable> _disposablesOnShow = new();

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(OnClickClose);
            CloseWidget = OnClickClose;
        }

        public override void Initialize()
        {
            base.Initialize();
            submitButton.Text = L10nManager.Localize("UI_WORLD_MAP_ENTER");

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

            L10nManager.OnLanguageChange
                .Subscribe(_ => submitButton.Text = L10nManager.Localize("UI_WORLD_MAP_ENTER"))
                .AddTo(gameObject);

            Game.Event.OnRoomEnter.AddListener(b => Close(true));
        }

        private static void ShowTooltip(StageRewardItemView view)
        {
            AudioController.PlayClick();
            ItemTooltip.Find(view.Data.ItemType)
                .Show(view.Data, string.Empty, false, null);
        }

        private void OnClickClose()
        {
            if (_stageType == StageType.Mimisbrunnr)
            {
                Game.Event.OnRoomEnter.Invoke(true);
            }

            Close(true);
        }

        private void RefreshSeasonPassCourageAmount(bool isEventDungeon = false)
        {
            if(Game.Game.instance.SeasonPassServiceManager.CurrentSeasonPassData != null)
            {
                foreach (var item in seasonPassObjs)
                {
                    item.SetActive(true);
                }

                if (isEventDungeon)
                {
                    seasonPassCourageAmount.text = $"+{Game.Game.instance.SeasonPassServiceManager.EventDungeonCourageAmount}";
                }
                else
                {
                    seasonPassCourageAmount.text = $"+{Game.Game.instance.SeasonPassServiceManager.AdventureCourageAmount}";
                }
            }
            else
            {
                foreach (var item in seasonPassObjs)
                {
                    item.SetActive(false);
                }
            }
        }

        public void Show(
            WorldMap.ViewModel viewModel,
            WorldSheet.Row worldRow,
            StageType stageType)
        {
            _stageType = stageType;

            _disposablesOnShow.DisposeAllAndClear();
            _sharedViewModel = viewModel;
            _sharedViewModel.WorldInformation.TryGetWorld(worldRow.Id, out var worldModel);
            _sharedViewModel.SelectedStageId.Subscribe(selectedStageId =>
            {
                UpdateStageInformationForWorld(
                    selectedStageId,
                    States.Instance.CurrentAvatarState?.level ?? 1);
            }).AddTo(_disposablesOnShow);

            closeButtonText.text = L10nManager.Localize($"WORLD_NAME_{worldModel.Name.ToUpper()}");

            if (_sharedViewModel.SelectedStageId.Value == 1)
            {
                stageHelpButton.Show();
            }
            else
            {
                stageHelpButton.Hide();
            }

            world.Set(worldRow, _stageType);
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

                world.Set(openedStageId, worldModel.GetNextStageId());
            }
            else
            {
                world.Set(-1, world.SharedViewModel.RowData.StageBegin);
            }

            base.Show(true);
            world.ShowByStageId(_sharedViewModel.SelectedStageId.Value, questStageId);
            RefreshSeasonPassCourageAmount();
        }

        public void Show(
            WorldMap.ViewModel viewModel,
            EventDungeonSheet.Row eventDungeonRow,
            int openedStageId,
            int nextStageId)
        {
            _stageType = StageType.EventDungeon;
            _disposablesOnShow.DisposeAllAndClear();
            _sharedViewModel = viewModel;
            _sharedViewModel.SelectedStageId
                .Subscribe(UpdateStageInformationForEventDungeon)
                .AddTo(_disposablesOnShow);

            closeButtonText.text = eventDungeonRow.GetLocalizedName();
            stageHelpButton.Hide();

            world.Set(eventDungeonRow);
            world.Set(openedStageId, nextStageId);
            world.ShowByStageId(_sharedViewModel.SelectedStageId.Value, nextStageId);
            RefreshSeasonPassCourageAmount(true);
            base.Show(true);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesOnShow.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        private void UpdateStageInfoSubmitButtonForWorld(int stageId)
        {
            var worldInfo = _sharedViewModel.WorldInformation;
            var isSubmittable = false;
            if (worldInfo is not null)
            {
                if (worldInfo.TryGetWorldByStageId(stageId, out var innerWorld))
                {
                    isSubmittable = innerWorld.IsPlayable(stageId);
                }
                else
                {
                    // NOTE: Consider expanding the world.
                    if (TableSheets.Instance.WorldSheet.TryGetByStageId(
                            stageId,
                            out var worldRow))
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
        }

        private void UpdateStageInfoSubmitButtonForEventDungeon(int eventDungeonStageId)
        {
            if (RxProps.EventDungeonRow is null)
            {
                submitButton.Interactable = false;
                return;
            }

            if (RxProps.EventDungeonInfo.Value is null ||
                RxProps.EventDungeonInfo.Value.ClearedStageId == 0)
            {
                submitButton.Interactable =
                    eventDungeonStageId == RxProps.EventDungeonRow.StageBegin;
                return;
            }

            submitButton.Interactable =
                eventDungeonStageId <=
                math.min(
                    RxProps.EventDungeonInfo.Value.ClearedStageId + 1,
                    RxProps.EventDungeonRow.StageEnd);
        }

        private void UpdateStageInfoMonsters(List<int> monsterIds)
        {
            var monsterCount = monsterIds.Count;
            for (var i = 0; i < monstersAreaCharacterViews.Count; i++)
            {
                var characterView = monstersAreaCharacterViews[i];
                if (i < monsterCount)
                {
                    characterView.Show();
                    characterView.SetByCharacterId(monsterIds[i]);

                    continue;
                }

                characterView.Hide();
            }
        }

        private void UpdateStageInfoRewards(List<MaterialItemSheet.Row> rewardItemRows)
        {
            var rewardItemCount = rewardItemRows.Count;
            for (var i = 0; i < rewardsAreaItemViews.Count; i++)
            {
                var itemView = rewardsAreaItemViews[i];
                if (i < rewardItemCount)
                {
                    itemView.Show();
                    itemView.SetData(rewardItemRows[i], () => ShowTooltip(itemView));
                    continue;
                }

                itemView.Hide();
            }
        }

        private void UpdateStageInformationForWorld(int stageId, int characterLevel)
        {
            UpdateStageInfoSubmitButtonForWorld(stageId);

            var stageWaveSheet = TableSheets.Instance.StageWaveSheet;
            stageWaveSheet.TryGetValue(stageId, out var stageWaveRow, true);
            var stageText = GetStageIdString(
                _stageType,
                stageWaveRow.StageId,
                true);
            titleText.text = $"Stage {stageText}";
            UpdateStageInfoMonsters(stageWaveRow.TotalMonsterIds);

            var stageSheet = TableSheets.Instance.StageSheet;
            stageSheet.TryGetValue(stageId, out var stageRow, true);
            UpdateStageInfoRewards(stageRow.GetRewardItemRows());

            var exp = StageRewardExpHelper.GetExp(characterLevel, stageId);
            expText.text = $"EXP +{exp}";

            buttonNotification.SetActive(stageId == Find<WorldMap>().StageIdToNotify);
        }

        private void UpdateStageInformationForEventDungeon(int eventDungeonStageId)
        {
            UpdateStageInfoSubmitButtonForEventDungeon(eventDungeonStageId);

            var stageRow = RxProps.EventDungeonStageRows.FirstOrDefault(e =>
                e.Id == eventDungeonStageId);
            if (stageRow is null ||
                !TableSheets.Instance.EventDungeonStageWaveSheet
                    .TryGetValue(stageRow.Id, out var stageWaveRow))
            {
                titleText.text = string.Empty;
                UpdateStageInfoMonsters(new List<int>());

                return;
            }

            titleText.text = $"Stage {stageRow.GetStageNumber()}";
            UpdateStageInfoMonsters(stageWaveRow.TotalMonsterIds);
            UpdateStageInfoRewards(stageRow.GetRewardItemRows());

            var exp = RxProps.EventScheduleRowForDungeon.Value.GetStageExp(
                eventDungeonStageId.ToEventDungeonStageNumber(),
                1);
            expText.text = $"EXP +{exp}";

            buttonNotification.SetActive(false);
        }

        private void GoToPreparation()
        {
            int stageNumber;
            HeaderMenuStatic.AssetVisibleState headerMenuState;
            switch (_stageType)
            {
                case StageType.HackAndSlash:
                {
                    stageNumber = _sharedViewModel.SelectedStageId.Value;
                    headerMenuState = HeaderMenuStatic.AssetVisibleState.Battle;
                    break;
                }
                case StageType.Mimisbrunnr:
                {
                    stageNumber = _sharedViewModel.SelectedStageId.Value % 10000000;
                    headerMenuState = HeaderMenuStatic.AssetVisibleState.Battle;
                    break;
                }
                case StageType.EventDungeon:
                {
                    stageNumber = _sharedViewModel.SelectedStageId.Value
                        .ToEventDungeonStageNumber();
                    headerMenuState = HeaderMenuStatic.AssetVisibleState.EventDungeon;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Find<BattlePreparation>().Show(
                _stageType,
                _sharedViewModel.SelectedWorldId.Value,
                _sharedViewModel.SelectedStageId.Value,
                $"{closeButtonText.text} {stageNumber}",
                true);
            Find<HeaderMenuStatic>().UpdateAssets(headerMenuState);
            Find<HeaderMenuStatic>().Show(true);
        }

        public static string GetStageIdString(
            StageType stageType,
            int stageId,
            bool isTitle = false)
        {
            switch (stageType)
            {
                case StageType.HackAndSlash:
                case StageType.AdventureBoss:
                    return stageId.ToString(CultureInfo.InvariantCulture);
                case StageType.Mimisbrunnr:
                    var enter = isTitle ? string.Empty : "\n";
                    return $"<sprite name=icon_Element_1>{enter}{stageId % 10000000}";
                case StageType.EventDungeon:
                    return stageId.ToEventDungeonStageNumber()
                        .ToString(CultureInfo.InvariantCulture);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
