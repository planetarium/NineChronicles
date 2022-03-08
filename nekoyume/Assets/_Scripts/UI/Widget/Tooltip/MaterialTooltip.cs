using System.Collections.Generic;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI
{
    public class MaterialTooltip : ItemTooltip
    {
        private static List<StageSheet.Row> GetStageByOrder(
            IOrderedEnumerable<StageSheet.Row> rows,
            int id)
        {
            var result = new List<StageSheet.Row>();
            // (int order, StageSheet.Row row) result = (0, null);
            var rowList = rows.ToList();

            rowList = rowList.Where(s =>
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

                Debug.Log("select in cleared stage");
                return result;
            }

            Debug.Log("select in not cleared stage");
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

        public override void Show(RectTransform target, InventoryItem item, string submitText, bool interactable,
            System.Action onSubmit, System.Action onClose = null, System.Action onBlocked = null)
        {
            base.Show(target, item, submitText, interactable, onSubmit, onClose, onBlocked);
            var stageRowList = Game.Game.instance.TableSheets.StageSheet
                .GetStagesContainsReward(item.ItemBase.Id)
                .OrderByDescending(s => s.Key);

            var stages = GetStageByOrder(stageRowList, item.ItemBase.Id);

            Debug.Log(
                $"stageRow.Id : {stages[0].Id}, world name : {L10nManager.LocalizeWorldName(stages[0].Id)}");
            Debug.Log(
                $"stageRow.Id : {stages[1].Id}, world name : {L10nManager.LocalizeWorldName(stages[1].Id)}");
        }
    }
}
