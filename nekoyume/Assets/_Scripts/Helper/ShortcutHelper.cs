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

            MobileShop, // Shop icon is same as ShopPC.
            Upgrade, // Upgrade icon is same as Craft.
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
            else switch (itemSubType)
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
                    // Hourglass and AP Stone can get in both platform.
#if UNITY_ANDROID || UNITY_IOS
                    var shopByPlatform = PlaceType.MobileShop;
#else
                    var shopByPlatform = PlaceType.PCShop;
#endif
                    if (!isTradable.HasValue)
                    {
                        acquisitionPlaceList.AddRange(new[]
                        {
                            GetAcquisitionPlace(caller, shopByPlatform),
                            GetAcquisitionPlace(caller, PlaceType.Staking),
                            GetAcquisitionPlace(caller, PlaceType.Quest)
                        });
                        break;
                    }

                    if (isTradable.Value)
                    {
                        acquisitionPlaceList.AddRange(new[]
                        {
                            GetAcquisitionPlace(caller, shopByPlatform),
                            GetAcquisitionPlace(caller, PlaceType.Staking),
                        });
                    }
                    else
                    {
                        acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.Quest));
                    }

                    break;
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
            600202, // Ruby Dust
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
                var categorySchemas = Widget.Find<MobileShop>().CachedCategorySchemas;
                var canBuyInMobileShop = categorySchemas
                    .Where(c => c.Active && c.Name != "NoShow")
                    .Any(c => c.ProductList
                        .Where(p => p.Active && p.Buyable)
                        .Any(p => p.FungibleItemList
                            .Any(fi => fi.SheetItemId == itemRow.Id)));

                if (canBuyInMobileShop)
                {
                    acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.MobileShop));
                }

#endif
                acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.Staking));
            }

            switch (itemRow.ItemType)
            {
                case ItemType.Equipment:
                    var canCraft = itemRow.ItemSubType != ItemSubType.Aura;

                    if (required)
                    {
                        acquisitionPlaceList.Add(GetAcquisitionPlace(caller, PlaceType.Upgrade));
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
                        GetAcquisitionPlace(caller, PlaceType.PCShop),
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
            ItemSheet.Row itemRow = null)
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
                        Widget.Find<MobileShop>().Show();
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
                        Widget.Find<Enhancement>().Show();
                    };
                    guideText = L10nManager.Localize("UI_UPGRADE_EQUIPMENT");
                    break;
                case PlaceType.Summon:
                    shortcutAction = () =>
                    {
                        caller.CloseWithOtherWidgets();
                        Widget.Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Summon);
                        Widget.Find<Summon>().Show();
                    };
                    guideText = L10nManager.Localize("UI_SUMMON");
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
            }).ToList();

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
                rowList = stageRows.ToList();
                for (var i = rowList.Count - 1; i >= 0 && result.Count < MaxCountOfAcquisitionStages; i--)
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

        /// <summary>
        /// Check the shortcut of model is available.
        /// </summary>
        public static bool CheckConditionOfShortcut(PlaceType type, int stageId = 0)
        {
            switch (type)
            {
                case PlaceType.EventDungeonStage:
                    var playableStageId =
                        RxProps.EventDungeonInfo.HasValue ||
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
                    return !Platform.IsMobilePlatform() &&
                           States.Instance.CurrentAvatarState.worldInformation
                               .IsStageCleared(Game.LiveAsset.GameConfig.RequiredStage.Shop);
                case PlaceType.MobileShop:
                    return Platform.IsMobilePlatform();
                case PlaceType.Arena:
                    return States.Instance.CurrentAvatarState.worldInformation
                        .IsStageCleared(Game.LiveAsset.GameConfig.RequiredStage.Arena);
                case PlaceType.Quest:
                case PlaceType.Staking:
                    return true;
                case PlaceType.Craft:
                case PlaceType.Upgrade:
                case PlaceType.Summon:
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
                PlaceType.PCShop => !Game.Game.instance.IsInWorld,
                PlaceType.MobileShop => !Game.Game.instance.IsInWorld,
                PlaceType.Arena => !Game.Game.instance.IsInWorld,
                PlaceType.Quest => !Widget.Find<BattleResultPopup>().IsActive() &&
                                   !Widget.Find<RankingBattleResultPopup>().IsActive(),
                PlaceType.Staking => true,
                PlaceType.Craft => true,
                PlaceType.Upgrade => true,
                PlaceType.Summon => true,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        #endregion
    }
}
