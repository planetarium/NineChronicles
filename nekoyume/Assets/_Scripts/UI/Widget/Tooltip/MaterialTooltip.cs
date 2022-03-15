using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amazon.CloudWatchLogs.Model.Internal.MarshallTransformations;
using Nekoyume.EnumType;
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

        public override void Show(
            RectTransform target,
            ItemBase item,
            string submitText,
            bool interactable,
            System.Action onSubmit,
            System.Action onClose = null,
            System.Action onBlocked = null,
            int itemCount = 0)
        {
            base.Show(target,
                item,
                submitText,
                interactable,
                onSubmit,
                onClose,
                onBlocked,
                itemCount);
            acquisitionGroup.SetActive(false);
            SetAcquisitionPlaceButtons(item);
        }

        public override void Show(RectTransform target,
            ShopItem item,
            System.Action onRegister,
            System.Action onSellCancellation,
            System.Action onClose)
        {
            base.Show(target, item, onRegister, onSellCancellation, onClose);
            acquisitionGroup.SetActive(false);
        }

        public override void Show(RectTransform target,
            ShopItem item,
            System.Action onBuy,
            System.Action onClose)
        {
            base.Show(target, item, onBuy, onClose);
            acquisitionGroup.SetActive(false);
        }

        public override void Show(RectTransform target,
            InventoryItem item,
            string submitText,
            bool interactable,
            System.Action onSubmit,
            System.Action onClose = null,
            System.Action onBlocked = null)
        {
            Show(target, item.ItemBase, submitText, interactable, onSubmit, onClose, onBlocked,
                item.Count.Value);
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
                var row = rowList.FirstOrDefault();
                if (row != null)
                {
                    result.Add(row);
                    rowList.Remove(row);
                }

                var secondRow = rowList
                    .OrderByDescending(r =>
                        r.Rewards.Find(reward => reward.ItemId == id).Ratio)
                    .FirstOrDefault();
                if (secondRow != null)
                {
                    result.Add(secondRow);
                }

                return result;
            }

            return rows.Take(2).ToList();
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
                                    AcquisitionPlaceButton.PlaceType.Stage, itemBase, row.Id,
                                    stage);
                            }

                            return null;
                        }));
                    }

                    break;
                case ItemSubType.FoodMaterial:
                    acquisitionPlaceList.Add(
                        MakeAcquisitionPlaceModelByType(AcquisitionPlaceButton.PlaceType.Arena,
                            itemBase));

                    break;
                case ItemSubType.Hourglass:
                case ItemSubType.ApStone:
                    var isTradable = itemBase is ITradableItem;
                    if (isTradable)
                    {
                        acquisitionPlaceList.Add(MakeAcquisitionPlaceModelByType(AcquisitionPlaceButton.PlaceType.Shop, itemBase));
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
                        acquisitionPlaceList.Add(new AcquisitionPlaceButton.Model(
                            AcquisitionPlaceButton.PlaceType.Staking, () => { },
                            L10nManager.Localize("UI_PLACE_STAKING"),
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
                    acquisitionPlaceList.Add(MakeAcquisitionPlaceModelByType(AcquisitionPlaceButton.PlaceType.Arena, itemBase));
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

        private void CloseOtherWidgets()
        {
            var deletableWidgets = FindWidgets().Where(widget =>
                !(widget is SystemWidget) &&
                !(widget is MessageCatTooltip) &&
                !(widget is Menu) &&
                !(widget is HeaderMenuStatic) &&
                !(widget is MaterialTooltip) &&
                !(widget is ShopBuy) &&
                !(widget is ShopSell) &&
                widget.IsActive());
            foreach (var widget in deletableWidgets)
            {
                widget.Close(true);
            }

            Find<ShopBuy>().Close(true, true);
            Find<ShopSell>().Close(true, true);
            Find<EventBanner>().Close(true);
            Find<Status>().Close(true);
            Close(true);
        }

        private AcquisitionPlaceButton.Model MakeAcquisitionPlaceModelByType(
            AcquisitionPlaceButton.PlaceType type,
            ItemBase itemBase,
            int worldId = 0,
            StageSheet.Row stageRow = null)
        {
            return type switch
            {
                AcquisitionPlaceButton.PlaceType.Stage => new AcquisitionPlaceButton.Model(
                    AcquisitionPlaceButton.PlaceType.Stage,
                    () =>
                    {
                        CloseOtherWidgets();
                        Game.Game.instance.Stage.GetPlayer().gameObject.SetActive(false);

                        var worldMap = Find<WorldMap>();
                        worldMap.Show(States.Instance.CurrentAvatarState.worldInformation);
                        worldMap.Show(worldId, stageRow.Id, false);
                        worldMap.SharedViewModel.WorldInformation.TryGetWorld(worldId,
                            out var worldModel);

                        Find<BattlePreparation>().Show(StageType.HackAndSlash,
                            worldMap.SharedViewModel.SelectedWorldId.Value,
                            worldMap.SharedViewModel.SelectedStageId.Value,
                            $"{L10nManager.Localize($"WORLD_NAME_{worldModel.Name.ToUpper()}")} {worldMap.SharedViewModel.SelectedStageId.Value}",
                            true);
                    },
                    $"{L10nManager.LocalizeWorldName(worldId)} {stageRow.Id % 10_000_000}",
                    itemBase,
                    stageRow),
                AcquisitionPlaceButton.PlaceType.Shop => new AcquisitionPlaceButton.Model(
                    AcquisitionPlaceButton.PlaceType.Shop, () =>
                    {
                        CloseOtherWidgets();
                        Find<HeaderMenuStatic>()
                            .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
                        var shopBuy = Find<ShopBuy>();
                        shopBuy.Show();
                    },
                    L10nManager.Localize("UI_MAIN_MENU_SHOP"),
                    itemBase),
                AcquisitionPlaceButton.PlaceType.Arena => new AcquisitionPlaceButton.Model(
                    AcquisitionPlaceButton.PlaceType.Arena, () =>
                    {
                        CloseOtherWidgets();
                        Find<HeaderMenuStatic>()
                            .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
                        Find<RankingBoard>().Show();
                    },
                    L10nManager.Localize("UI_MAIN_MENU_RANKING"), itemBase),
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
                    AcquisitionPlaceButton.PlaceType.Staking, () => { },
                    L10nManager.Localize("UI_PLACE_STAKING"),
                    itemBase),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}
