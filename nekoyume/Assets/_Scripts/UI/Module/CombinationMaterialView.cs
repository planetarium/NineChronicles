using Nekoyume.UI.Model;
using UniRx;

namespace Nekoyume.UI.Module
{
    public class CombinationMaterialView : CountEditableItemView<CombinationMaterial>
    {
        public bool IsLocked { get; private set; }
        
        public InventoryItem InventoryItemViewModel { get; private set; }
        
        public void Set(InventoryItemView inventoryItem)
        {
            if (inventoryItem is null ||
                inventoryItem.Model is null ||
                inventoryItem.Model.ItemBase.Value is null)
            {
                InventoryItemViewModel = null;
                base.Clear();

                return;
            }

            InventoryItemViewModel = inventoryItem.Model;
            var model = new CombinationMaterial(
                inventoryItem.Model.ItemBase.Value,
                1,
                1,
                inventoryItem.Model.Count.Value);
            base.SetData(model);
        }

        public void Unlock()
        {
            // 열린 배경. 버튼 활성화.
//            backgroundImage.sprite
            itemButton.interactable = true;

            if (Model is null)
                return;
            
            IsLocked = false;
        }

        public void Lock()
        {
            // 잠긴 배경. 버튼 비활성화.
//            backgroundImage.sprite
            itemButton.interactable = false;
            
            if (Model is null)
                return;
            
            IsLocked = true;
        }
    }
}
