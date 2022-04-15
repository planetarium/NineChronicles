using System;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class GrindModule : MonoBehaviour
    {
        [SerializeField]
        private Inventory grindInventory;

        [SerializeField]
        private ConditionalCostButton grindButton;

        public void Initialize()
        {
            grindInventory.SetGrinding(ShowItemTooltip);
        }

        private void ShowItemTooltip(InventoryItem model, RectTransform target)
        {
            var tooltip = ItemTooltip.Find(model.ItemBase.ItemType);
            tooltip.Show(
                model,
                "Grind",
                model.ItemBase is ITradableItem,
                () => RegisterToGrindingList(model),
                grindInventory.ClearSelectedItem,
                target: target);
        }

        private void RegisterToGrindingList(InventoryItem item)
        {

        }
    }
}
