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

namespace Nekoyume.Helper
{
    public static class ShortcutHelper
    {
        private const int MaxCountOfAcquisitionStages = 2;

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
                        AcquisitionPlaceButton.PlaceType.Arena,
                        itemBase)
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
                            AcquisitionPlaceButton.PlaceType.Stage,
                            itemBase,
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
                                    AcquisitionPlaceButton.PlaceType.EventDungeonStage,
                                    itemBase,
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
                            AcquisitionPlaceButton.PlaceType.Shop,
                            AcquisitionPlaceButton.PlaceType.Staking
                        }.Select(type =>
                            MakeAcquisitionPlaceModelByPlaceType(
                                caller,
                                type,
                                itemBase))
                    );
                }
                else
                {
                    acquisitionPlaceList.Add(
                        MakeAcquisitionPlaceModelByPlaceType(caller,
                            AcquisitionPlaceButton.PlaceType.Quest,
                            itemBase)
                    );
                }
            }

            if (!acquisitionPlaceList.Any() ||
                acquisitionPlaceList.All(model =>
                    model.Type != AcquisitionPlaceButton.PlaceType.Quest))
            {
                // If can get this item from quest...
                if (States.Instance.CurrentAvatarState.questList.Any(quest =>
                        !quest.Complete && quest.Reward.ItemMap.ContainsKey(itemBase.Id)))
                {
                    acquisitionPlaceList.Add(
                        MakeAcquisitionPlaceModelByPlaceType(
                            caller,
                            AcquisitionPlaceButton.PlaceType.Quest,
                            itemBase));
                }
            }

            return acquisitionPlaceList;
        }

        public static AcquisitionPlaceButton.Model MakeAcquisitionPlaceModelByPlaceType(
            Widget caller,
            AcquisitionPlaceButton.PlaceType type,
            ItemBase itemBase,
            int worldId = 0,
            StageSheet.Row stageRow = null)
        {
            System.Action shortcutAction = caller.CloseWithOtherWidgets;
            string guideText;
            switch (type)
            {
                case AcquisitionPlaceButton.PlaceType.Stage:
                    if (stageRow is null)
                    {
                        throw new Exception($"{nameof(stageRow)} is null");
                    }

                    shortcutAction += () => ShortcutActionForStage(worldId, stageRow);
                    guideText =
                        $"{L10nManager.LocalizeWorldName(worldId)} {stageRow.Id % 10_000_000}";
                    break;
                case AcquisitionPlaceButton.PlaceType.EventDungeonStage:
                    if (stageRow is null)
                    {
                        throw new Exception($"{nameof(stageRow)} is null");
                    }

                    shortcutAction += () => ShortcutActionForEventStage(stageRow);
                    guideText =
                        $"{RxProps.EventDungeonRow.GetLocalizedName()} {stageRow.Id.ToEventDungeonStageNumber()}";
                    break;
                case AcquisitionPlaceButton.PlaceType.Shop:
                    shortcutAction += () =>
                    {
                        Widget.Find<HeaderMenuStatic>()
                            .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
                        var shopBuy = Widget.Find<ShopBuy>();
                        shopBuy.Show();
                    };
                    guideText = L10nManager.Localize("UI_MAIN_MENU_SHOP");
                    break;
                case AcquisitionPlaceButton.PlaceType.Arena:
                    shortcutAction += () =>
                    {
                        Widget.Find<HeaderMenuStatic>()
                            .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
                        Widget.Find<ArenaJoin>().ShowAsync().Forget();
                    };
                    guideText = L10nManager.Localize("UI_MAIN_MENU_RANKING");
                    break;
                case AcquisitionPlaceButton.PlaceType.Quest:
                    shortcutAction += () =>
                    {
                        Widget.Find<AvatarInfoPopup>().Close();
                        Widget.Find<QuestPopup>().Show();
                    };
                    guideText = L10nManager.Localize("UI_QUEST");
                    break;
                case AcquisitionPlaceButton.PlaceType.Staking:
                    shortcutAction += () => Widget.Find<StakingPopup>().Show();
                    guideText = L10nManager.Localize("UI_PLACE_STAKING");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            return new AcquisitionPlaceButton.Model(
                type,
                shortcutAction,
                guideText,
                itemBase,
                stageRow
            );
        }

        public static void ShortcutActionForStage(
            int worldId,
            StageSheet.Row stageRow)
        {
            Game.Game.instance.Stage.GetPlayer().gameObject.SetActive(false);
            var worldMap = Widget.Find<WorldMap>();
            worldMap.Show(worldId, stageRow.Id, false);
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
                    true);
            Widget.Find<HeaderMenuStatic>()
                .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
        }

        public static void ShortcutActionForEventStage(
            StageSheet.Row stageRow)
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
                    stageRow.Id,
                    $"{RxProps.EventDungeonRow?.GetLocalizedName()} {stageRow.Id.ToEventDungeonStageNumber()}",
                    true);
            Widget.Find<HeaderMenuStatic>()
                .UpdateAssets(HeaderMenuStatic.AssetVisibleState.EventDungeon);
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
                            return stageRow.Id <= RxProps.EventDungeonInfo.Value.ClearedStageId;
                        }

                        return stageRow.Id.ToEventDungeonStageNumber() <= 1;
                    }

                    States.Instance.CurrentAvatarState.worldInformation
                        .TryGetLastClearedStageId(out var lastClearedStageId);
                    return stageRow.Id <= lastClearedStageId;
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
                     i >= 0 && result.Count >= MaxCountOfAcquisitionStages;
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
