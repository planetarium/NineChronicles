using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.EnumType;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Game.Battle;
using Nekoyume.Game.LiveAsset;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.WorldBoss;
using Nekoyume.UI.Scroller;

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
            Craft = 1,
            PCShop = 3,
            Arena = 4,
            Quest = 5,
            Staking = 7,
            EventDungeonStage = 8,
            Summon = 12,
            AdventureBoss = 13,
            WorldBoss = 14,
            Grinding = 15,

            MobileShop, // Shop icon is same as ShopPC.
            Upgrade // Upgrade icon is same as Craft.
        }

#region AcquisitionPlace

        // For Material
        public static List<AcquisitionPlaceButton.Model> GetAcquisitionPlaceList(
            Widget caller,
            int itemId,
            ItemSubType itemSubType,
            bool? isTradable = null)
        {
            var acquisitionPlaceList = new List<AcquisitionPlaceButton.Model>();

            // For FoodMaterial or Arena season medal
            if (TableSheets.Instance.WeeklyArenaRewardSheet.Values
                .Any(row => row.Reward.ItemId == itemId))
            {
                acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.Arena));
            }
            else
            {
                if (Action.ItemEnhancement.HammerIds.Contains(itemId))
                {
                    acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.AdventureBoss));
                }

                switch (itemSubType)
                {
                    case ItemSubType.EquipmentMaterial
                        or ItemSubType.MonsterPart
                        or ItemSubType.NormalMaterial:
                    {
                        var stages = TableSheets.Instance.StageSheet
                            .GetStagesContainsReward(itemId)
                            .OrderStagesByPriority(itemId)
                            .Select(stage =>
                            {
                                TableSheets.Instance.WorldSheet.TryGetByStageId(stage.Id, out var worldRow);
                                return GetAcquisitionPlace(caller, PlaceType.Stage, (worldRow.Id, stage.Id));
                            });

                        acquisitionPlaceList.AddRange(stages);
                        break;
                    }
                    case ItemSubType.Hourglass or ItemSubType.ApStone:
                        AcquisitionPlaceButton.Model shopByPlatform = null;
                        // Hourglass and AP Stone can get in both platform.
#if UNITY_ANDROID || UNITY_IOS
                    if (Game.Game.instance.IAPStoreManager.TryGetCategoryName(itemId, out var categoryName))
                    {
                        shopByPlatform = GetAcquisitionPlace(caller, PlaceType.MobileShop,
                            categoryName: categoryName);
                    }
#else
                        shopByPlatform = GetAcquisitionPlace(caller, PlaceType.PCShop);
#endif

                        if (!isTradable.HasValue || isTradable.Value)
                        {
                            if (shopByPlatform != null)
                            {
                                acquisitionPlaceList.Add(shopByPlatform);
                            }

                            acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.Staking));
                        }

                        if (!isTradable.HasValue || !isTradable.Value)
                        {
                            acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.Quest));
                        }

                        break;
                    case ItemSubType.Circle:
                    {
                        acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.AdventureBoss));
                        acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.WorldBoss));
                        acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.Grinding));
                        acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.PCShop));

                        var stageRows = TableSheets.Instance.StageSheet
                            .GetStagesContainsReward(itemId)
                            .OrderStagesByPriority(itemId);
                        foreach (var stageRow in stageRows)
                        {
                            TableSheets.Instance.WorldSheet.TryGetByStageId(stageRow.Id, out var worldRow);
                            acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.Stage, (worldRow.Id, stageRow.Id)));
                        }

                        break;
                    }
                    case ItemSubType.Scroll:
                        acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.Grinding));
                        acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.PCShop));
                        acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.Staking));
                        break;
                }
            }

            if (RxProps.EventScheduleRowForDungeon.HasValue)
            {
                var stages = RxProps.EventDungeonStageRows
                    .GetStagesContainsReward(itemId)
                    .OrderStagesByPriority(itemId, true)
                    .Select(stage => GetAcquisitionPlace(caller, PlaceType.EventDungeonStage,
                        (RxProps.EventDungeonRow.Id, stage.Id)));

                acquisitionPlaceList.AddRange(stages);
            }

            if (!acquisitionPlaceList.Any() ||
                acquisitionPlaceList.All(model => model.Type != PlaceType.Quest))
            {
                // If can get this item from quest...
                if (States.Instance.CurrentAvatarState.questList.Any(quest =>
                    !quest.Complete && quest.Reward.ItemMap.Any(r => r.Item1 == itemId)))
                {
                    acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.Quest));
                }
            }

            return acquisitionPlaceList;
        }

        private static readonly int[] ShopItemIds =
        {
            800201, // Silver Dust
            600201, // Golden Dust
            600202 // Ruby Dust
        };

        public static List<AcquisitionPlaceButton.Model> GetAcquisitionPlaceList(
            Widget caller,
            ItemSheet.Row itemRow,
            bool required)
        {
            var acquisitionPlaceList = new List<AcquisitionPlaceButton.Model>();

            if (ShopItemIds.Contains(itemRow.Id))
            {
#if UNITY_ANDROID || UNITY_IOS
                if (Game.Game.instance.IAPStoreManager.TryGetCategoryName(itemRow.Id, out var categoryName))
                {
                    acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.MobileShop,
                        categoryName: categoryName));
                }

#endif
                acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.Staking));
            }

            switch (itemRow.ItemType)
            {
                case ItemType.Equipment:
                    var canCraft = itemRow.ItemSubType is not (ItemSubType.Aura or ItemSubType.Grimoire);

                    if (required)
                    {
                        acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.Upgrade, itemRow: itemRow));
                    }
                    else if (canCraft)
                    {
                        acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.Craft, itemRow: itemRow));
                    }
                    else
                    {
                        acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.Summon));
                    }

                    if (canCraft)
                    {
                        acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.PCShop));
                    }

                    break;
                case ItemType.Consumable:
                    acquisitionPlaceList.AddRange(new[]
                    {
                        GetAcquisitionPlace(caller, PlaceType.Craft, itemRow: itemRow),
                        GetAcquisitionPlace(caller, PlaceType.PCShop)
                    });
                    break;
                case ItemType.Costume:
                    acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.PCShop));
                    break;
                case ItemType.Material:
                    acquisitionPlaceList.AddRange(GetAcquisitionPlaceList(caller, itemRow.Id, itemRow.ItemSubType));
                    break;
            }

            return acquisitionPlaceList;
        }

        public static AcquisitionPlaceButton.Model GetAcquisitionPlace(
            Widget caller,
            PlaceType type,
            (int worldId, int stageId)? stageInfo = null,
            ItemSheet.Row itemRow = null,
            string categoryName = null)
        {
            System.Action shortcutAction;
            string guideText;
            switch (type)
            {
                case PlaceType.Stage:
                    if (!stageInfo.HasValue)
                    {
                        throw new Exception($"{nameof(stageInfo.Value)} is null");
                    }

                    shortcutAction = () =>
                    {
                        caller.CloseWithOtherWidgets();
                        ShortcutActionForStage(stageInfo.Value.worldId, stageInfo.Value.stageId);
                    };
                    guideText = $"{L10nManager.LocalizeWorldName(stageInfo.Value.worldId)} {stageInfo.Value.stageId % 10_000_000}";
                    break;
                case PlaceType.EventDungeonStage:
                    if (!stageInfo.HasValue)
                    {
                        throw new Exception($"{nameof(stageInfo.Value)} is null");
                    }

                    shortcutAction = () =>
                    {
                        caller.CloseWithOtherWidgets();
                        ShortcutActionForEventStage(stageInfo.Value.stageId);
                    };
                    guideText = $"{RxProps.EventDungeonRow.GetLocalizedName()} {stageInfo.Value.stageId.ToEventDungeonStageNumber()}";
                    break;
                case PlaceType.PCShop:
                    shortcutAction = () =>
                    {
                        caller.CloseWithOtherWidgets();
                        Widget.Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
                        Widget.Find<ShopBuy>().Show();
                    };
                    guideText = L10nManager.Localize("UI_SHOP_PC");
                    break;
                case PlaceType.MobileShop:
                    shortcutAction = () =>
                    {
                        caller.CloseWithOtherWidgets();
                        Widget.Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
                        Widget.Find<MobileShop>().ShowAsTab(categoryName).Forget();
                    };
                    guideText = L10nManager.Localize("UI_SHOP_MOBILE");
                    break;
                case PlaceType.Arena:
                    shortcutAction = () =>
                    {
                        caller.CloseWithOtherWidgets();
                        Widget.Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
                        Widget.Find<ArenaJoin>().ShowAsync().Forget();
                    };
                    guideText = L10nManager.Localize("UI_MAIN_MENU_RANKING");
                    break;
                case PlaceType.Quest:
                    shortcutAction = () =>
                    {
                        if (caller is ItemTooltip)
                        {
                            caller.Close();
                        }

                        Widget.Find<AvatarInfoPopup>().Close();
                        Widget.Find<QuestPopup>().Show();
                    };
                    guideText = L10nManager.Localize("UI_QUEST");
                    break;
                case PlaceType.Staking:
                    shortcutAction = () =>
                    {
                        if (caller is ItemTooltip)
                        {
                            caller.Close();
                        }

                        Widget.Find<StakingPopup>().Show();
                    };
                    guideText = L10nManager.Localize("UI_PLACE_STAKING");
                    break;
                case PlaceType.Craft:
                    shortcutAction = () =>
                    {
                        caller.CloseWithOtherWidgets();
                        Widget.Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
                        Widget.Find<Craft>().ShowWithItemRow(itemRow);
                    };
                    guideText = L10nManager.Localize("CRAFT");
                    break;
                case PlaceType.Upgrade:
                    shortcutAction = () =>
                    {
                        caller.CloseWithOtherWidgets();
                        Widget.Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
                        Widget.Find<Enhancement>().Show(itemRow);
                    };
                    guideText = L10nManager.Localize("UI_UPGRADE_EQUIPMENT");
                    break;
                case PlaceType.Summon:
                    shortcutAction = () =>
                    {
                        caller.CloseWithOtherWidgets();
                        Widget.Find<Summon>().Show();
                    };
                    guideText = L10nManager.Localize("UI_SUMMON");
                    break;
                case PlaceType.AdventureBoss:
                    shortcutAction = () => ShortcutActionForAdventureBoss(caller);
                    guideText = L10nManager.Localize("UI_ADVENTURE_BODD_BACK_BUTTON");
                    break;
                case PlaceType.WorldBoss:
                    shortcutAction = () => ShortcutActionForWorldBoss(caller);
                    guideText = L10nManager.Localize("UI_MAIN_MENU_WORLDBOSS");
                    break;
                case PlaceType.Grinding:
                    shortcutAction = () =>
                    {
                        caller.CloseWithOtherWidgets();
                        Widget.Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
                        Widget.Find<Grind>().Show();
                    };
                    guideText = L10nManager.Localize("GRIND_UI_BUTTON");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            return new AcquisitionPlaceButton.Model(
                type,
                shortcutAction,
                guideText,
                stageInfo?.stageId ?? 0);
        }

        /// <summary>
        /// Select recommended stages of Acquisition from StageSheet Rows by priority.
        /// </summary>
        /// <param name="stageRows">Stages can get itemId.</param>
        /// <param name="itemId">ID of the item you want to get.</param>
        /// <param name="isEventStageRows">Flag for Event stages.</param>
        /// <returns>In stageRows, select up to two recommended stages.</returns>
        private static IEnumerable<StageSheet.Row> OrderStagesByPriority(
            this IReadOnlyCollection<StageSheet.Row> stageRows,
            int itemId,
            bool isEventStageRows = false)
        {
            States.Instance.CurrentAvatarState.worldInformation
                .TryGetLastClearedStageId(out var lastClearedStageId);

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

                return stageRow.Id <= lastClearedStageId + 1;
            }).ToList();

            // If 'rowList' contains cleared stage
            if (rowList.Any())
            {
                // First recommended stage is the highest level in rowList.
                rowList = rowList.OrderByDescending(row => row.Key).ToList();
                var row = rowList.First();
                result.Add(row);
                rowList.Remove(row);

                // Second recommended stage is that has highest getting ratio in rowList.
                var secondRow = rowList
                    .OrderByDescending(row1 => row1.Rewards.Find(reward => reward.ItemId == itemId).Ratio)
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
                rowList = stageRows.OrderBy(row => row.Key).ToList();
                for (var i = 0; i < rowList.Count && result.Count < MaxCountOfAcquisitionStages; i++)
                {
                    if (!result.Contains(rowList[i]))
                    {
                        result.Add(rowList[i]);
                    }
                }
            }

            return result;
        }

#endregion

#region Shortcut

        public static void ShortcutActionForStage(
            int worldId,
            int stageId,
            bool showByGuideQuest = false)
        {
            Game.Game.instance.Stage.GetPlayer().gameObject.SetActive(false);

            var worldMap = Widget.Find<WorldMap>();
            worldMap.SetWorldInformation(States.Instance.CurrentAvatarState.worldInformation);
            worldMap.Show(worldId, stageId, false);
            worldMap.SharedViewModel.WorldInformation.TryGetWorld(worldId, out var worldModel);

            Widget.Find<BattlePreparation>().Show(
                StageType.HackAndSlash,
                worldMap.SharedViewModel.SelectedWorldId.Value,
                worldMap.SharedViewModel.SelectedStageId.Value,
                $"{L10nManager.Localize($"WORLD_NAME_{worldModel.Name.ToUpper()}")} {worldMap.SharedViewModel.SelectedStageId.Value}",
                true,
                showByGuideQuest);
            Widget.Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
        }

        public static void ShortcutActionForEventStage(
            int eventDungeonStageId,
            bool showByGuideQuest = false)
        {
            Game.Game.instance.Stage.GetPlayer().gameObject.SetActive(false);

            var worldMap = Widget.Find<WorldMap>();
            worldMap.SetWorldInformation(States.Instance.CurrentAvatarState.worldInformation);
            worldMap.ShowEventDungeonStage(RxProps.EventDungeonRow, false);

            Widget.Find<HeaderMenuStatic>().Show(true);
            Widget.Find<BattlePreparation>().Show(
                StageType.EventDungeon,
                worldMap.SharedViewModel.SelectedWorldId.Value,
                eventDungeonStageId,
                $"{RxProps.EventDungeonRow?.GetLocalizedName()} {eventDungeonStageId.ToEventDungeonStageNumber()}",
                true,
                showByGuideQuest);
            Widget.Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.EventDungeon);
        }

        public static void ShortcutActionForAdventureBoss(Widget caller)
        {
            var adventureBossData = Game.Game.instance.AdventureBossData;
            var worldInformation = States.Instance.CurrentAvatarState.worldInformation;
            switch (adventureBossData.CurrentState.Value)
            {
                case AdventureBossData.AdventureBossSeasonState.Ready:
                    caller.CloseWithOtherWidgets();
                    Widget.Find<WorldMap>().Show(worldInformation, true);
                    Widget.Find<AdventureBossEnterBountyPopup>().Show();
                    break;
                case AdventureBossData.AdventureBossSeasonState.Progress:
                    caller.CloseWithOtherWidgets();
                    Widget.Find<WorldMap>().Show(worldInformation, true);
                    WorldMapAdventureBoss.OnClickOpenAdventureBoss();
                    break;
                case AdventureBossData.AdventureBossSeasonState.End:
                    if (adventureBossData.EndedSeasonInfos.TryGetValue(
                        adventureBossData.SeasonInfo.Value.Season, out var endedSeasonInfo))
                    {
                        var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
                        var secondsPerBlock = Nekoyume.Helper.Util.BlockInterval;

                        var remainBlock = endedSeasonInfo.NextStartBlockIndex - currentBlockIndex;
                        var nextStartTime = endedSeasonInfo.NextStartBlockIndex
                            .BlockIndexToDateTimeString(currentBlockIndex, secondsPerBlock, DateTime.Now, "yyyy/MM/dd HH:mm");
                        var message = L10nManager.Localize("UI_ADVENTUREBOSS_SEASON_ENDED",
                            remainBlock, remainBlock.BlockRangeToTimeSpanString(), nextStartTime);

                        OneLineSystem.Push(
                            MailType.System,
                            message,
                            NotificationCell.NotificationType.Alert);
                    }

                    break;
            }
        }

        public static void ShortcutActionForWorldBoss(Widget caller)
        {
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            var worldBossStatus = WorldBossFrontHelper.GetStatus(currentBlockIndex);
            switch (worldBossStatus)
            {
                case WorldBossStatus.Season:
                    caller.CloseWithOtherWidgets();
                        Widget.Find<WorldBoss>().ShowAsync().Forget();
                    break;
                case WorldBossStatus.OffSeason:
                    if (WorldBossFrontHelper.TryGetNextRow(currentBlockIndex, out var next))
                    {
                        var remainBlock = next.StartedBlockIndex - currentBlockIndex;
                        var secondPerBlock = Util.BlockInterval;
                        var nextStartTime = next.StartedBlockIndex
                            .BlockIndexToDateTimeString(currentBlockIndex, secondPerBlock, DateTime.Now, "yyyy/MM/dd HH:mm");
                        var message = L10nManager.Localize("UI_ADVENTUREBOSS_SEASON_ENDED",
                            remainBlock, remainBlock.BlockRangeToTimeSpanString(), nextStartTime);
                        OneLineSystem.Push(
                            MailType.System,
                            message,
                            NotificationCell.NotificationType.Alert);
                    }
                    else
                    {
                        OneLineSystem.Push(
                            MailType.System,
                            "There is no world boss schedule.",
                            NotificationCell.NotificationType.Alert);
                    }
                    break;
            }
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
                        !RxProps.EventDungeonInfo.HasValue ||
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
                case PlaceType.PCShop:
#if UNITY_ANDROID || UNITY_IOS
                    return false;
#else
                    return States.Instance.CurrentAvatarState.worldInformation
                        .IsStageCleared(Game.LiveAsset.GameConfig.RequiredStage.Shop);
#endif
                case PlaceType.MobileShop:
#if UNITY_ANDROID || UNITY_IOS
                    return true;
#else
                    return false;
#endif
                case PlaceType.Arena:
                    return States.Instance.CurrentAvatarState.worldInformation
                        .IsStageCleared(Game.LiveAsset.GameConfig.RequiredStage.Arena);
                case PlaceType.Quest:
                case PlaceType.Staking:
                case PlaceType.Craft:
                case PlaceType.Upgrade:
                case PlaceType.Summon:
                case PlaceType.Grinding:
                    return true;
                case PlaceType.AdventureBoss:
                    return !Game.LiveAsset.GameConfig.IsKoreanBuild;
                case PlaceType.WorldBoss:
                    return States.Instance.CurrentAvatarState.worldInformation
                        .IsStageCleared(Game.LiveAsset.GameConfig.RequiredStage.WorldBoss);
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
                PlaceType.Stage => !BattleRenderer.Instance.IsOnBattle,
                PlaceType.EventDungeonStage => !BattleRenderer.Instance.IsOnBattle,
                PlaceType.PCShop => !BattleRenderer.Instance.IsOnBattle,
                PlaceType.MobileShop => !BattleRenderer.Instance.IsOnBattle,
                PlaceType.Arena => !BattleRenderer.Instance.IsOnBattle,
                PlaceType.Quest => !Widget.Find<BattleResultPopup>().IsActive() &&
                    !Widget.Find<RankingBattleResultPopup>().IsActive(),
                PlaceType.Staking => true,
                PlaceType.Craft => true,
                PlaceType.Upgrade => true,
                PlaceType.Summon => true,
                PlaceType.AdventureBoss => !BattleRenderer.Instance.IsOnBattle,
                PlaceType.WorldBoss => !BattleRenderer.Instance.IsOnBattle,
                PlaceType.Grinding => true,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

#endregion
    }
}
