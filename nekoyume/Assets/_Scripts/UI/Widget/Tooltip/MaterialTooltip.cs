using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amazon.CloudWatchLogs.Model.Internal.MarshallTransformations;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using static Nekoyume.UI.Widget;
using Event = Nekoyume.Game.Event;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI
{
    public class MaterialTooltip : ItemTooltip
    {
        [SerializeField]
        protected GameObject acquisitionGroup;

        public const string StakingDescriptionUrl =
            "https://ninechronicles.medium.com/monster-collection-muspelheim-the-realm-of-fire-part-2-b5c36e089b81";

        public override void Show(
            ItemBase item,
            string submitText,
            bool interactable,
            System.Action onSubmit,
            System.Action onClose = null,
            System.Action onBlocked = null,
            int itemCount = 0,
            RectTransform target = null)
        {
            base.Show(
                item,
                submitText,
                interactable,
                onSubmit,
                onClose,
                onBlocked,
                itemCount,
                target);
            acquisitionGroup.SetActive(false);
            SetAcquisitionPlaceButtons(item);
        }

        public override void Show(
            ShopItem item,
            System.Action onRegister,
            System.Action onSellCancellation,
            System.Action onClose,
            RectTransform target = null)
        {
            base.Show(item, onRegister, onSellCancellation, onClose, target);
            acquisitionGroup.SetActive(false);
        }

        public override void Show(
            ShopItem item,
            System.Action onBuy,
            System.Action onClose,
            RectTransform target)
        {
            base.Show(item, onBuy, onClose, target);
            acquisitionGroup.SetActive(false);
        }

        public override void Show(
            InventoryItem item,
            string submitText,
            bool interactable,
            System.Action onSubmit,
            System.Action onClose = null,
            System.Action onBlocked = null,
            RectTransform target = null)
        {
            Show(
                item.ItemBase,
                submitText,
                interactable,
                onSubmit,
                onClose,
                onBlocked,
                item.Count.Value,
                target);
        }

        private static List<StageSheet.Row> GetStageByOrder(
            IOrderedEnumerable<StageSheet.Row> rows,
            int id)
        {
            var result = new List<StageSheet.Row>();
            var rowList = rows.Where(s =>
            {
                if (States.Instance.CurrentAvatarState.worldInformation
                    .TryGetUnlockedWorldByStageClearedBlockIndex(out var world))
                {
                    return s.Id <= world.StageClearedId;
                }

                return false;
            }).ToList();

            if (rowList.Any())
            {
                rowList = rowList.OrderByDescending(sheet => sheet.Key).ToList();
                var row = rowList.FirstOrDefault();
                if (row != null)
                {
                    result.Add(row);
                    rowList.Remove(row);
                }

                var secondRow = rowList
                    .OrderByDescending(r =>
                        r.Rewards.Find(reward => reward.ItemId == id).Ratio)
                    .ThenByDescending(sheet => sheet.Key)
                    .FirstOrDefault();
                if (secondRow != null)
                {
                    result.Add(secondRow);
                }
            }

            rowList = rows.ToList();
            var rowCount = rowList.Count;
            for (int i = rowCount - 1; i >= 0; i--)
            {
                if (result.Count >= 2)
                {
                    break;
                }

                if (Game.Game.instance.TableSheets.WorldSheet.TryGetByStageId(rowList[i].Id, out _))
                {
                    if (!result.Contains(rowList[i]))
                    {
                        result.Add(rowList[i]);
                    }
                }
            }

            return result;
        }

        private void SetAcquisitionPlaceButtons(ItemBase itemBase)
        {
            acquisitionPlaceButtons.ForEach(button => button.gameObject.SetActive(false));
            var acquisitionPlaceList = new List<AcquisitionPlaceButton.Model>();

            switch (itemBase.ItemSubType)
            {
                case ItemSubType.EquipmentMaterial:
                case ItemSubType.MonsterPart:
                case ItemSubType.NormalMaterial:
                    var stageRowList = Game.Game.instance.TableSheets.StageSheet
                        .GetStagesContainsReward(itemBase.Id)
                        .OrderByDescending(s => s.Key);
                    var stages = GetStageByOrder(stageRowList, itemBase.Id);
                    // Acquisition place is stage...
                    if (stages.Any())
                    {
                        acquisitionPlaceList.AddRange(stages.Select(stage =>
                        {
                            if (Game.Game.instance.TableSheets.WorldSheet.TryGetByStageId(stage.Id,
                                out var row))
                            {
                                return MakeAcquisitionPlaceModelByType(
                                    AcquisitionPlaceButton.PlaceType.Stage,
                                    itemBase,
                                    row.Id,
                                    stage);
                            }

                            return null;
                        }));
                    }

                    break;
                case ItemSubType.FoodMaterial:
                    acquisitionPlaceList.Add(
                        MakeAcquisitionPlaceModelByType(
                            AcquisitionPlaceButton.PlaceType.Arena,
                            itemBase));

                    break;
                case ItemSubType.Hourglass:
                case ItemSubType.ApStone:
                    var isTradable = itemBase is ITradableItem;
                    if (isTradable)
                    {
                        acquisitionPlaceList.Add(MakeAcquisitionPlaceModelByType(
                            AcquisitionPlaceButton.PlaceType.Shop,
                            itemBase));
                        acquisitionPlaceList.Add(
                            MakeAcquisitionPlaceModelByType(
                                AcquisitionPlaceButton.PlaceType.Staking, itemBase));
                    }
                    else
                    {
                        acquisitionPlaceList.Add(new AcquisitionPlaceButton.Model(
                            AcquisitionPlaceButton.PlaceType.Quest, () =>
                            {
                                Close();
                                Find<AvatarInfoPopup>().Close();
                                Find<QuestPopup>().Show();
                            },
                            L10nManager.Localize("UI_QUEST"),
                            itemBase));
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (acquisitionPlaceList.All(model =>
                    model.Type != AcquisitionPlaceButton.PlaceType.Arena) ||
                acquisitionPlaceList.Count == 0)
            {
                if (Game.Game.instance.TableSheets.WeeklyArenaRewardSheet.Any(pair =>
                    pair.Value.Reward.ItemId == itemBase.Id))
                {
                    acquisitionPlaceList.Add(MakeAcquisitionPlaceModelByType(
                        AcquisitionPlaceButton.PlaceType.Arena,
                        itemBase));
                }
            }

            if (acquisitionPlaceList.All(model =>
                model.Type != AcquisitionPlaceButton.PlaceType.Quest &&
                model.ItemBase.ItemSubType != ItemSubType.Hourglass &&
                model.ItemBase.ItemSubType != ItemSubType.ApStone))
            {
                // Acquisition place is quest...
                if (States.Instance.CurrentAvatarState.questList.Any(quest =>
                    !quest.Complete && quest.Reward.ItemMap.ContainsKey(itemBase.Id)))
                {
                    acquisitionPlaceList.Add(
                        MakeAcquisitionPlaceModelByType(AcquisitionPlaceButton.PlaceType.Quest,
                            itemBase));
                }
            }

            var placeCount = acquisitionPlaceList.Count;
            if (placeCount > 0)
            {
                acquisitionGroup.SetActive(true);
                for (int i = 0; i < placeCount && i < 4; i++)
                {
                    acquisitionPlaceButtons[i].gameObject.SetActive(true);
                    acquisitionPlaceButtons[i].Set(acquisitionPlaceList[i]);
                }
            }
        }

        private AcquisitionPlaceButton.Model MakeAcquisitionPlaceModelByType(
            AcquisitionPlaceButton.PlaceType type,
            ItemBase itemBase,
            int worldId = 0,
            StageSheet.Row stageRow = null)
        {
            return type switch
            {
                AcquisitionPlaceButton.PlaceType.Stage => stageRow is null
                    ? throw new Exception($"{nameof(stageRow)} is null")
                    : new AcquisitionPlaceButton.Model(
                        AcquisitionPlaceButton.PlaceType.Stage,
                        () =>
                        {
                            CloseWithOtherWidgets();
                            Game.Game.instance.Stage.GetPlayer().gameObject.SetActive(false);

                            var worldMap = Find<WorldMap>();
                            worldMap.SetWorldInformation(States.Instance.CurrentAvatarState.worldInformation);
                            worldMap.Show(worldId, stageRow.Id, false);
                            worldMap.SharedViewModel.WorldInformation.TryGetWorld(worldId, out var worldModel);

                            var isMimisbrunnrWorld = worldId == GameConfig.MimisbrunnrWorldId;
                            var stageNum = isMimisbrunnrWorld
                                ? worldMap.SharedViewModel.SelectedStageId.Value % 10000000
                                : worldMap.SharedViewModel.SelectedStageId.Value;
                            Find<BattlePreparation>().Show(
                                isMimisbrunnrWorld ? StageType.Mimisbrunnr : StageType.HackAndSlash,
                                worldMap.SharedViewModel.SelectedWorldId.Value,
                                worldMap.SharedViewModel.SelectedStageId.Value,
                                $"{L10nManager.Localize($"WORLD_NAME_{worldModel.Name.ToUpper()}")} {stageNum}",
                                true);
                        },
                        $"{L10nManager.LocalizeWorldName(worldId)} {stageRow.Id % 10_000_000}",
                        itemBase,
                        stageRow),
                AcquisitionPlaceButton.PlaceType.Arena => new AcquisitionPlaceButton.Model(
                    AcquisitionPlaceButton.PlaceType.Arena, () =>
                    {
                        CloseWithOtherWidgets();
                        Find<HeaderMenuStatic>()
                            .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
                        Find<ArenaJoin>().Show();
                    },
                    L10nManager.Localize("UI_MAIN_MENU_RANKING"), itemBase),
                AcquisitionPlaceButton.PlaceType.Shop => new AcquisitionPlaceButton.Model(
                    AcquisitionPlaceButton.PlaceType.Shop, () =>
                    {
                        CloseWithOtherWidgets();
                        Find<HeaderMenuStatic>()
                            .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
                        var shopBuy = Find<ShopBuy>();
                        shopBuy.Show();
                    },
                    L10nManager.Localize("UI_MAIN_MENU_SHOP"),
                    itemBase),
                AcquisitionPlaceButton.PlaceType.Quest => new AcquisitionPlaceButton.Model(
                    AcquisitionPlaceButton.PlaceType.Quest, () =>
                    {
                        Close();
                        Find<AvatarInfoPopup>().Close();
                        Find<QuestPopup>().Show();
                    },
                    L10nManager.Localize("UI_QUEST"),
                    itemBase),
                AcquisitionPlaceButton.PlaceType.Staking => new AcquisitionPlaceButton.Model(
                    AcquisitionPlaceButton.PlaceType.Staking, () => { Application.OpenURL(StakingDescriptionUrl); },
                    L10nManager.Localize("UI_PLACE_STAKING"),
                    itemBase),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}
