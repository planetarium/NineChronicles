using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class ElementalCombinationMaterialPanel : CombinationMaterialPanel
    {
        [SerializeField]
        private EquipmentOptionView optionView;

        public override void SetData(EquipmentItemRecipeSheet.Row row, int? subRecipeId)
        {
            var equipmentSheet = Game.Game.instance.TableSheets.EquipmentItemSheet;
            if (!equipmentSheet.TryGetValue(row.ResultEquipmentId, out var equipmentRow))
            {
                Debug.LogWarning($"Equipment ID not found : {row.ResultEquipmentId}");
                return;
            }
            optionView.Show(equipmentRow.GetLocalizedName(), subRecipeId.Value, true);

            base.SetData(row, subRecipeId);
        }
    }
}
