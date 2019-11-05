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
            if (!(inventoryItemViewModel?.ItemBase.Value is Equipment equipment))
            {
                Clear();
                return;
            }

            base.Set(inventoryItemViewModel);

            itemNameText.enabled = true;
            itemNameText.text = equipment.GetLocalizedName();
            UpdateStatView();
        }

        public void UpdateStatView(string additionalValueText = null)
        {
            if (!(Model?.ItemBase.Value is Equipment equipment))
                return;

            equipment.TryGetBaseStat(out var type, out var value, true);            
            if (string.IsNullOrEmpty(additionalValueText))
            {
                statView.Show(type, value);
            }
            else
            {
                statView.Show(type.ToString(), $"{value}{additionalValueText}");
            }
        }

        public override void Clear()
        {
            itemNameText.enabled = false;
            statView.Hide();
            base.Clear();
        }
    }
}
