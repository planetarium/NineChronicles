using Nekoyume.UI.Model;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CombinationMaterialView : CountEditableItemView<CombinationMaterial>, ILockable
    {
        public Image[] effectImages;

        public bool IsLocked => !itemButton.interactable;

        public InventoryItem InventoryItemViewModel { get; private set; }

        public void Set(InventoryItemView inventoryItemView)
        {
            if (inventoryItemView is null)
            {
                Clear();
                return;
            }
            
            Set(inventoryItemView.Model);
        }

        public void Set(InventoryItem inventoryItemViewModel)
        {
            if (inventoryItemViewModel is null ||
                inventoryItemViewModel.ItemBase.Value is null)
            {
                Clear();
                return;
            }

            var model = new CombinationMaterial(
                inventoryItemViewModel.ItemBase.Value,
                1,
                1,
                inventoryItemViewModel.Count.Value);
            base.SetData(model);
            SetEnableEffectImages(true);
            InventoryItemViewModel = inventoryItemViewModel;
        }

        public override void Clear()
        {
            InventoryItemViewModel = null;
            SetEnableEffectImages(false);
            base.Clear();
        }

        public void Lock()
        {
            Clear();
            itemButton.interactable = false;
//            backgroundImage.sprite
        }
        
        public void Unlock()
        {
            itemButton.interactable = true;
//            backgroundImage.sprite
        }

        public void UnlockAsNCG()
        {
            itemButton.interactable = true;
//            backgroundImage.sprite
        }

        private void SetEnableEffectImages(bool enable)
        {
            foreach (var effectImage in effectImages)
            {
                effectImage.enabled = enable;
            }
        }
    }
}
