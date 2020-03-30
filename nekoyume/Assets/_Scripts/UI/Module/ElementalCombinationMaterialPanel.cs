using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class ElementalCombinationMaterialPanel : CombinationMaterialPanel
    {
        [SerializeField]
        private EquipmentOptionView optionView;

        public override void SetData(
            EquipmentItemRecipeSheet.Row row,
            int? subRecipeId,
            bool checkInventory = true
        )
        {
            var equipmentSheet = Game.Game.instance.TableSheets.EquipmentItemSheet;
            if (!equipmentSheet.TryGetValue(row.ResultEquipmentId, out var equipmentRow))
            {
                Debug.LogWarning($"Equipment ID not found : {row.ResultEquipmentId}");
                return;
            }

            if (subRecipeId.HasValue)
            {
                optionView.Show(equipmentRow.GetLocalizedName(), subRecipeId.Value);
                optionView.SetDimmed(false);
            }
            else
            {
                optionView.Hide();
            }

            base.SetData(row, subRecipeId, checkInventory);
        }
    }
}
