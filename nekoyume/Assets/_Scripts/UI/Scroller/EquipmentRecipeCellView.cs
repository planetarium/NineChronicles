using Nekoyume.Model.Item;
using Nekoyume.Model.State;
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
            {
                return;
            }

            var equipmentSheet = Game.Game.instance.TableSheets.EquipmentItemSheet;
            if (!equipmentSheet.TryGetValue(recipeRow.ResultEquipmentId, out var row))
            {
                return;
            }

            RowData = recipeRow;

            var equipment = (Equipment) ItemFactory.CreateItemUsable(row, Guid.Empty, default);
            Set(equipment);

            StatType = equipment.UniqueStatType;
            var text = $"{equipment.Stat.Type} +{equipment.Stat.Value}";
            optionText.text = text;
            SetLocked(false, RowData.UnlockStage);
        }

        public void Set(AvatarState avatarState)
        {
            if (RowData is null)
            {
                return;
            }

            // 해금 검사.
            if (!avatarState.worldInformation.IsStageCleared(RowData.UnlockStage))
            {
                SetLocked(true, RowData.UnlockStage);
                return;
            }

            SetLocked(false, RowData.UnlockStage);

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
    }
}
