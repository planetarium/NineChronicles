using Nekoyume.Game.Item;
using Nekoyume.UI.Model;
using TMPro;

namespace Nekoyume.UI.Module
{
    public class EnhancementMaterialView : CombinationMaterialView
    {
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI itemNameText;
        public StatView statView;
        
        public override void Set(InventoryItem inventoryItemViewModel)
        {
            if (inventoryItemViewModel is null ||
                inventoryItemViewModel.ItemBase.Value is null ||
                !(inventoryItemViewModel.ItemBase.Value is Equipment equipment))
            {
                Clear();
                return;
            }

            base.Set(inventoryItemViewModel);

            itemNameText.enabled = true;
            itemNameText.text = equipment.GetLocalizedName();
            statView.Show(equipment.Data.Stat.Type,
                equipment.StatsMap.GetValue(equipment.Data.Stat.Type, true));
        }
        
        public override void Clear()
        {
            itemNameText.enabled = false;
            statView.Hide();
            base.Clear();
        }
    }
}
