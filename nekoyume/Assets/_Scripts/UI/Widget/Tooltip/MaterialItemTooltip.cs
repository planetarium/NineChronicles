using System.Linq;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI
{
    public class MaterialItemTooltip : ItemTooltip
    {
        public override void Show(RectTransform target, InventoryItem item, string submitText, bool interactable,
            System.Action onSubmit, System.Action onClose = null, System.Action onBlocked = null)
        {
            base.Show(target, item, submitText, interactable, onSubmit, onClose, onBlocked);
            var stageRowList = Util.GetStagesByItemId(
                Game.Game.instance.TableSheets.StageSheet,
                item.ItemBase.Id).OrderByDescending(s => s.Key);
            var clearedStageRows = stageRowList.Where(s =>
            {
                if (States.Instance.CurrentAvatarState.worldInformation
                    .TryGetUnlockedWorldByStageClearedBlockIndex(out var world))
                {
                    return s.Id <= world.StageClearedId;
                }

                return false;
            }).ToList();
            var firstStageRow = clearedStageRows.First();

            Debug.LogWarning(
                $"stageRow.Id : {firstStageRow.Id}, world name : {TextHelper.GetWorldNameByStageId(firstStageRow.Id)}");
        }
    }
}
