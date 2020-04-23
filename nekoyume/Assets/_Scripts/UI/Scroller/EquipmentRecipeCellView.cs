using Assets.SimpleLocalization;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using System;
using System.Linq;

namespace Nekoyume.UI.Scroller
{
    public class EquipmentRecipeCellView : RecipeCellView
    {
        public EquipmentItemRecipeSheet.Row RowData { get; private set; }

        public void Set(EquipmentItemRecipeSheet.Row recipeRow)
        {
            if (recipeRow is null)
                return;

            var equipmentSheet = Game.Game.instance.TableSheets.EquipmentItemSheet;
            if (!equipmentSheet.TryGetValue(recipeRow.ResultEquipmentId, out var row))
                return;

            RowData = recipeRow;

            var equipment = (Equipment) ItemFactory.CreateItemUsable(row, Guid.Empty, default);
            Set(equipment);

            StatType = equipment.UniqueStatType;
            var text = $"{equipment.Data.Stat.Type} +{equipment.Data.Stat.Value}";
            optionText.text = text;
            SetLocked(false);
        }

        public void Set(AvatarState avatarState)
        {
            if (RowData is null)
                return;

            // 해금 검사.
            if (!avatarState.worldInformation.IsStageCleared(RowData.UnlockStage))
            {
                SetLocked(true);
                return;
            }

            SetLocked(false);

            // 메인 재료 검사.
            var inventory = avatarState.inventory;
            var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
            if (materialSheet.TryGetValue(RowData.MaterialId, out var materialRow) &&
                inventory.TryGetMaterial(materialRow.ItemId, out var fungibleItem) &&
                fungibleItem.count >= RowData.MaterialCount)
            {
                // 서브 재료 검사.
                if (RowData.SubRecipeIds.Any())
                {
                    var subSheet = Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheet;
                    var shouldDimmed = false;
                    foreach (var subRow in RowData.SubRecipeIds
                        .Select(subRecipeId =>
                            subSheet.TryGetValue(subRecipeId, out var subRow) ? subRow : null)
                        .Where(item => !(item is null)))
                    {
                        foreach (var info in subRow.Materials)
                        {
                            if (materialSheet.TryGetValue(info.Id, out materialRow) &&
                                inventory.TryGetMaterial(materialRow.ItemId,
                                    out fungibleItem) &&
                                fungibleItem.count >= info.Count)
                            {
                                continue;
                            }

                            shouldDimmed = true;
                            break;
                        }

                        if (!shouldDimmed)
                        {
                            break;
                        }
                    }

                    SetDimmed(shouldDimmed);
                }
                else
                {
                    SetDimmed(false);
                }
            }
            else
            {
                SetDimmed(true);
            }
        }

        protected void SetLocked(bool value)
        {
            // TODO: 나중에 해금 시스템이 분리되면 아래의 해금 조건 텍스트를 얻는 로직을 옮겨서 반복을 없애야 좋겠다.
            if (value)
            {
                unlockConditionText.enabled = true;

                if (RowData is null)
                {
                    unlockConditionText.text = string.Format(
                        LocalizationManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                        "???");
                }

                if (States.Instance.CurrentAvatarState.worldInformation.TryGetLastClearedStageId(
                    out var stageId))
                {
                    var diff = RowData.UnlockStage - stageId;
                    if (diff > 50)
                    {
                        unlockConditionText.text = string.Format(
                            LocalizationManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                            "???");
                    }
                    else
                    {
                        unlockConditionText.text = string.Format(
                            LocalizationManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                            RowData.UnlockStage.ToString());
                    }
                }
                else
                {
                    unlockConditionText.text = string.Format(
                        LocalizationManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                        "???");
                }
            }
            else
            {
                unlockConditionText.enabled = false;
            }

            SetCellViewLocked(value);
        }
    }
}
