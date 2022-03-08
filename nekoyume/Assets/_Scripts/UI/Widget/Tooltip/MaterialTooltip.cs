using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.CloudWatchLogs.Model.Internal.MarshallTransformations;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    public class MaterialTooltip : ItemTooltip
    {
        [SerializeField]
        private List<AcquisitionPlaceButton> acquisitionPlaceButtons;

        public override void Show(RectTransform target, InventoryItem item, string submitText, bool interactable,
            System.Action onSubmit, System.Action onClose = null, System.Action onBlocked = null)
        {
            base.Show(target, item, submitText, interactable, onSubmit, onClose, onBlocked);
            SetAcquisitionPlaceButtons(item.ItemBase);
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
                    .OrderByDescending(sheet => sheet.Key)
                    .ThenByDescending(r =>
                        r.Rewards.Find(reward => reward.ItemId == id).Ratio).FirstOrDefault();
                if (secondRow != null)
                {
                    result.Add(secondRow);
                }

                Debug.LogError("select in cleared stage");
                return result;
            }

            Debug.LogError("select in not cleared stage");
            return rows.Where(r =>
            {
                if (States.Instance.CurrentAvatarState.worldInformation
                    .TryGetUnlockedWorldByStageClearedBlockIndex(out var world))
                {
                    return r.Id > world.StageClearedId;
                }

                return false;
            }).OrderBy(sheet => sheet.Key).Take(2).ToList();
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
                        var worldSheet = Game.Game.instance.TableSheets.WorldSheet;
                        if (worldSheet.TryGetByStageId(stages[0].Id, out var row))
                        {
                            Debug.LogError(
                                $"stageRow.Id : {stages[0].Id}, world name : {L10nManager.LocalizeWorldName(row.Id)}");
                        }

                        if (worldSheet.TryGetByStageId(stages[1].Id, out row))
                        {
                            Debug.LogError(
                                $"stageRow.Id : {stages[1].Id}, world name : {L10nManager.LocalizeWorldName(row.Id)}");
                        }

                        acquisitionPlaceList.AddRange(stages.Select(stage =>
                        {
                            if (Game.Game.instance.TableSheets.WorldSheet.TryGetByStageId(stage.Id, out var row))
                            {
                                return new AcquisitionPlaceButton.Model(AcquisitionPlaceButton.PlaceType.Stage,
                                    null,
                                    $"{L10nManager.LocalizeWorldName(row.Id)} {stage.Id % 10_000_000}",
                                    itemBase,
                                    row);
                            }

                            return null;
                        }));
                    }
                    break;
                case ItemSubType.FoodMaterial:
                    acquisitionPlaceList.Add(new AcquisitionPlaceButton.Model(AcquisitionPlaceButton.PlaceType.Arena, null, "아레나", itemBase));
                    break;
                case ItemSubType.Hourglass:
                case ItemSubType.ApStone:
                    acquisitionPlaceList.Add(new AcquisitionPlaceButton.Model(AcquisitionPlaceButton.PlaceType.Quest, null, "퀘스트", itemBase));
                    acquisitionPlaceList.Add(new AcquisitionPlaceButton.Model(AcquisitionPlaceButton.PlaceType.Shop, null, "상점", itemBase));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (acquisitionPlaceList.All(model =>
                    model.Type != AcquisitionPlaceButton.PlaceType.Quest))
            {
                // Acquisition place is quest...
                if (States.Instance.CurrentAvatarState.questList.Any(quest => !quest.Complete && quest.Reward.ItemMap.ContainsKey(itemBase.Id)))
                {
                    acquisitionPlaceList.Add(new AcquisitionPlaceButton.Model(AcquisitionPlaceButton.PlaceType.Quest, null, "퀘스트", itemBase));
                }
            }

            var placeCount = acquisitionPlaceList.Count;
            for (int i = 0; i < placeCount && i < 4; i++)
            {
                acquisitionPlaceButtons[i].gameObject.SetActive(true);
                acquisitionPlaceButtons[i].Set(acquisitionPlaceList[i]);
            }

            Debug.LogError($"subtype : {itemBase.ItemSubType}");
        }
    }
}
