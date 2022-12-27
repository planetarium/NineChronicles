using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using UnityEngine;
using static Nekoyume.Helper.ShortcutHelper;

namespace Nekoyume.Helper
{
    public static class ShortcutHelper
    {
        private const int MaxCountOfAcquisitionStages = 2;

        public enum PlaceType
        {
            // Assigned values are used to load sprites.
            // Not used values: 1, 2, 6, etc.
            Stage,
            Shop = 3,
            Arena = 4,
            Quest = 5,
            Staking = 7,
            EventDungeonStage = 8,
        }

        public static List<AcquisitionPlaceButton.Model> GetAcquisitionPlaceList(
            Widget caller,
            ItemBase itemBase)
        {
            var acquisitionPlaceList = new List<AcquisitionPlaceButton.Model>();
            if (TableSheets.Instance.WeeklyArenaRewardSheet.Any(pair =>
                    pair.Value.Reward.ItemId == itemBase.Id))
            {
                acquisitionPlaceList.Add(
                    MakeAcquisitionPlaceModelByPlaceType(
                        caller,
                        PlaceType.Arena)
                );
            }
            else if (itemBase.ItemSubType is ItemSubType.EquipmentMaterial
                or ItemSubType.MonsterPart
                or ItemSubType.NormalMaterial)
            {
                var stageRowList = TableSheets.Instance.StageSheet
                    .GetStagesContainsReward(itemBase.Id)
                    .OrderByDescending(s => s.Key)
                    .ToList();
                var stages = SelectStagesByRecommendationPriority(stageRowList, itemBase.Id);
                if (stages.Any())
                {
                    acquisitionPlaceList.AddRange(stages.Select(stage =>
                    {
                        TableSheets.Instance.WorldSheet.TryGetByStageId(
                            stage.Id,
                            out var worldRow);
                        return MakeAcquisitionPlaceModelByPlaceType(
                            caller,
                            PlaceType.Stage,
                            worldRow.Id,
                            stage);
                    }));
                }
            }
            else if (itemBase.ItemSubType is ItemSubType.FoodMaterial)
            {
                var eventDungeonRows = RxProps
                    .EventDungeonStageRows
                    .GetStagesContainsReward(itemBase.Id)
                    .OrderByDescending(s => s.Key)
                    .ToList();
                if (eventDungeonRows.Any())
                {
                    var scheduleRow = RxProps.EventScheduleRowForDungeon.Value;
                    if (scheduleRow is not null)
                    {
                        var eventStages =
                            SelectStagesByRecommendationPriority(eventDungeonRows, itemBase.Id, true);
                        if (eventStages.Any())
                        {
                            acquisitionPlaceList.AddRange(eventStages.Select(stage =>
                                MakeAcquisitionPlaceModelByPlaceType(
                                    caller,
                                    PlaceType.EventDungeonStage,
                                    RxProps.EventDungeonRow.Id,
                                    stage))
                            );
                        }
                    }
                }
            }
            else if (itemBase.ItemSubType is ItemSubType.Hourglass
                     or ItemSubType.ApStone)
            {
                if (itemBase is ITradableItem)
                {
                    acquisitionPlaceList.AddRange(
                        new[]
                        {
                            PlaceType.Shop,
                            PlaceType.Staking
                        }.Select(type =>
                            MakeAcquisitionPlaceModelByPlaceType(
                                caller,
                                type))
                    );
                }
                else
                {
                    acquisitionPlaceList.Add(
                        MakeAcquisitionPlaceModelByPlaceType(caller,
                            PlaceType.Quest)
                    );
                }
            }

            if (!acquisitionPlaceList.Any() ||
                acquisitionPlaceList.All(model =>
                    model.Type != PlaceType.Quest))
            {
                // If can get this item from quest...
                if (States.Instance.CurrentAvatarState.questList.Any(quest =>
                        !quest.Complete && quest.Reward.ItemMap.ContainsKey(itemBase.Id)))
                {
                    acquisitionPlaceList.Add(
                        MakeAcquisitionPlaceModelByPlaceType(
                            caller,
                            PlaceType.Quest));
                }
            }

            return acquisitionPlaceList;
        }

        public static AcquisitionPlaceButton.Model MakeAcquisitionPlaceModelByPlaceType(
            Widget caller,
            PlaceType type,
            int worldId = 0,
            StageSheet.Row stageRow = null)
        {
            System.Action shortcutAction = caller.CloseWithOtherWidgets;
            string guideText;
            switch (type)
            {
                case PlaceType.Stage:
                    if (stageRow is null)
                    {
                        throw new Exception($"{nameof(stageRow)} is null");
                    }

                    shortcutAction += () => ShortcutActionForStage(worldId, stageRow.Id);
                    guideText =
                        $"{L10nManager.LocalizeWorldName(worldId)} {stageRow.Id % 10_000_000}";
                    break;
                case PlaceType.EventDungeonStage:
                    if (stageRow is null)
                    {
                        throw new Exception($"{nameof(stageRow)} is null");
                    }

                    shortcutAction += () => ShortcutActionForEventStage(stageRow.Id);
                    guideText =
                        $"{RxProps.EventDungeonRow.GetLocalizedName()} {stageRow.Id.ToEventDungeonStageNumber()}";
                    break;
                case PlaceType.Shop:
                    shortcutAction += () =>
                    {
                        Widget.Find<HeaderMenuStatic>()
                            .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
                        var shopBuy = Widget.Find<ShopBuy>();
                        shopBuy.Show();
                    };
                    guideText = L10nManager.Localize("UI_MAIN_MENU_SHOP");
                    break;
                case PlaceType.Arena:
                    shortcutAction += () =>
                    {
                        Widget.Find<HeaderMenuStatic>()
                            .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
                        Widget.Find<ArenaJoin>().ShowAsync().Forget();
                    };
                    guideText = L10nManager.Localize("UI_MAIN_MENU_RANKING");
                    break;
                case PlaceType.Quest:
                    shortcutAction = () =>
                    {
                        caller.Close();
                        Widget.Find<AvatarInfoPopup>().Close();
                        Widget.Find<QuestPopup>().Show();
                    };
                    guideText = L10nManager.Localize("UI_QUEST");
                    break;
                case PlaceType.Staking:
                    shortcutAction = () =>
                    {
                        caller.Close();
                        Application.OpenURL(StakingPopup.StakingUrl);
                    };
                    guideText = L10nManager.Localize("UI_PLACE_STAKING");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            return new AcquisitionPlaceButton.Model(
                type,
                shortcutAction,
                guideText,
                stageRow?.Id ?? 0
            );
        }

        public static void ShortcutActionForStage(
            int worldId,
            int stageId,
            bool showByGuideQuest = false)
        {
            Game.Game.instance.Stage.GetPlayer().gameObject.SetActive(false);
            var worldMap = Widget.Find<WorldMap>();
            worldMap.SetWorldInformation(States.Instance.CurrentAvatarState
                .worldInformation);
            worldMap.Show(worldId, stageId, false);
            worldMap.SharedViewModel.WorldInformation.TryGetWorld(worldId,
                out var worldModel);
            var isMimisbrunnrWorld = worldId == GameConfig.MimisbrunnrWorldId;
            var stageNum = isMimisbrunnrWorld
                ? worldMap.SharedViewModel.SelectedStageId.Value % 10000000
                : worldMap.SharedViewModel.SelectedStageId.Value;
            Widget.Find<BattlePreparation>()
                .Show(
                    isMimisbrunnrWorld
                        ? StageType.Mimisbrunnr
                        : StageType.HackAndSlash,
                    worldMap.SharedViewModel.SelectedWorldId.Value,
                    worldMap.SharedViewModel.SelectedStageId.Value,
                    $"{L10nManager.Localize($"WORLD_NAME_{worldModel.Name.ToUpper()}")} {stageNum}",
                    true,
                    showByGuideQuest);
            Widget.Find<HeaderMenuStatic>()
                .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
        }

        public static void ShortcutActionForEventStage(
            int eventDungeonStageId,
            bool showByGuideQuest = false)
        {
            Game.Game.instance.Stage.GetPlayer().gameObject.SetActive(false);
            var worldMap = Widget.Find<WorldMap>();
            worldMap.SetWorldInformation(States.Instance.CurrentAvatarState
                .worldInformation);
            worldMap.ShowEventDungeonStage(RxProps.EventDungeonRow, false);
            Widget.Find<HeaderMenuStatic>().Show(true);
            Widget.Find<BattlePreparation>()
                .Show(
                    StageType.EventDungeon,
                    worldMap.SharedViewModel.SelectedWorldId.Value,
                    eventDungeonStageId,
                    $"{RxProps.EventDungeonRow?.GetLocalizedName()} {eventDungeonStageId.ToEventDungeonStageNumber()}",
                    true,
                    showByGuideQuest);
            Widget.Find<HeaderMenuStatic>()
                .UpdateAssets(HeaderMenuStatic.AssetVisibleState.EventDungeon);
        }

        /// <summary>
        /// Check the shortcut of model is available.
        /// </summary>
        public static bool CheckConditionOfShortcut(PlaceType type, int stageId = 0)
        {
            switch (type)
            {
                case PlaceType.EventDungeonStage:
                    var playableStageId =
                        RxProps.EventDungeonInfo.Value is null ||
                        RxProps.EventDungeonInfo.Value.ClearedStageId == 0
                            ? RxProps.EventDungeonRow.StageBegin
                            : Math.Min(
                                RxProps.EventDungeonInfo.Value.ClearedStageId + 1,
                                RxProps.EventDungeonRow.StageEnd);
                    return stageId <= playableStageId;
                case PlaceType.Stage:
                    if (stageId == 1)
                    {
                        return true;
                    }

                    var sharedViewModel = Widget.Find<WorldMap>().SharedViewModel;
                    if (States.Instance.CurrentAvatarState.worldInformation
                        .TryGetWorldByStageId(stageId, out var world))
                    {
                        return stageId <= world.StageClearedId + 1 &&
                               sharedViewModel.UnlockedWorldIds.Contains(world.Id);
                    }

                    return false;
                case PlaceType.Shop:
                {
                    if (States.Instance.CurrentAvatarState.worldInformation
                        .TryGetLastClearedStageId(out var lastClearedStage))
                    {
                        return lastClearedStage >=
                               GameConfig.RequireClearedStageLevel.UIMainMenuShop;
                    }

                    return false;
                }

                case PlaceType.Arena:
                {
                    if (States.Instance.CurrentAvatarState.worldInformation
                        .TryGetLastClearedStageId(out var lastClearedStage))
                    {
                        return lastClearedStage >=
                               GameConfig.RequireClearedStageLevel.UIMainMenuRankingBoard;
                    }

                    return false;
                }
                case PlaceType.Quest:
                case PlaceType.Staking:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        /// <summary>
        /// Check condition to use shortcut by other UI.
        /// </summary>
        public static bool CheckUIStateForUsingShortcut(PlaceType type)
        {
            return type switch
            {
                PlaceType.Stage => !Game.Game.instance.IsInWorld,
                PlaceType.EventDungeonStage => !Game.Game.instance.IsInWorld,
                PlaceType.Shop => !Game.Game.instance.IsInWorld,
                PlaceType.Arena => !Game.Game.instance.IsInWorld,
                PlaceType.Quest => !Widget.Find<BattleResultPopup>().IsActive() &&
                                   !Widget.Find<RankingBattleResultPopup>().IsActive(),
                PlaceType.Staking => true,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        /// <summary>
        /// Select recommended stages from stageRows.
        /// </summary>
        /// <param name="stageRows">Stages can get itemId.</param>
        /// <param name="itemId">ID of the item you want to get.</param>
        /// <param name="isEventStageRows">Flag for Event stages.</param>
        /// <returns>In stageRows, select up to two recommended stages.</returns>
        private static List<StageSheet.Row> SelectStagesByRecommendationPriority(
            IEnumerable<StageSheet.Row> stageRows,
            int itemId,
            bool isEventStageRows = false)
        {
            var result = new List<StageSheet.Row>();
            var rowList = stageRows.Where(stageRow =>
                {
                    if (isEventStageRows)
                    {
                        if (RxProps.EventDungeonInfo.Value is not null)
                        {
                            return stageRow.Id <= RxProps.EventDungeonInfo.Value.ClearedStageId + 1;
                        }

                        return stageRow.Id.ToEventDungeonStageNumber() <= 1;
                    }

                    States.Instance.CurrentAvatarState.worldInformation
                        .TryGetLastClearedStageId(out var lastClearedStageId);
                    return stageRow.Id <= lastClearedStageId + 1;
                }
            ).ToList();

            // If 'stageRows' contains cleared stage
            if (rowList.Any())
            {
                // First recommended stage is the highest level in rowList.
                rowList = rowList.OrderByDescending(sheet => sheet.Key).ToList();
                var row = rowList.First();
                result.Add(row);
                rowList.Remove(row);

                // Second recommended stage is that has highest getting ratio in rowList.
                var secondRow = rowList
                    .OrderByDescending(row1 =>
                        row1.Rewards.Find(reward => reward.ItemId == itemId).Ratio)
                    .ThenByDescending(row2 => row2.Key)
                    .FirstOrDefault();
                if (secondRow != null)
                {
                    result.Add(secondRow);
                }
            }

            // If the number of results is insufficient,
            // add the closest stage among the stages that could not be cleared.
            if (result.Count < MaxCountOfAcquisitionStages)
            {
                rowList = stageRows.ToList();
                var rowCount = rowList.Count;
                for (var i = rowCount - 1;
                     i >= 0 && result.Count < MaxCountOfAcquisitionStages;
                     i--)
                {
                    if (!result.Contains(rowList[i]))
                    {
                        result.Add(rowList[i]);
                    }
                }
            }

            return result;
        }
    }
}
