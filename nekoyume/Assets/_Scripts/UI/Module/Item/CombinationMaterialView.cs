using Nekoyume.UI.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CombinationMaterialView : CountEditableItemView<CombinationMaterial>, ILockable
    {
        public Image ncgEffectImage;
        public Image effectImage;
        public Image[] effectImages;

        public bool IsLocked => !itemButton.interactable;

        public InventoryItem InventoryItemViewModel { get; private set; }
        public bool IsUnlockedAsNCG { get; private set; }

        public void Set(InventoryItemView inventoryItemView, int count = 1)
        {
            if (inventoryItemView is null)
            {
                Clear();
                return;
            }
            
            Set(inventoryItemView.Model, count);
        }

        public virtual void Set(InventoryItem inventoryItemViewModel, int count = 1)
        {
            if (inventoryItemViewModel is null ||
                inventoryItemViewModel.ItemBase.Value is null)
            {
                Clear();
                return;
            }

            var model = new CombinationMaterial(
                inventoryItemViewModel.ItemBase.Value,
                count,
                1,
                inventoryItemViewModel.Count.Value);
            base.SetData(model);
            SetEnableEffectImages(true);
            InventoryItemViewModel = inventoryItemViewModel;
        }

        public override void Clear()
        {
            InventoryItemViewModel = null;
            ncgEffectImage.enabled = IsUnlockedAsNCG;
            effectImage.enabled = false;
            SetEnableEffectImages(false);
            base.Clear();
        }

        public void Lock()
        {
            Clear();
            itemButton.interactable = false;
            backgroundImage.overrideSprite = Resources.Load<Sprite>("UI/Textures/ui_box_Inventory_05");
            backgroundImage.SetNativeSize();
            ncgEffectImage.enabled = false;
            effectImage.enabled = false;
        }
        
        public void Unlock()
        {
            itemButton.interactable = true;
            backgroundImage.overrideSprite = Resources.Load<Sprite>("UI/Textures/ui_box_Inventory_02");
            backgroundImage.SetNativeSize();
            ncgEffectImage.enabled = false;
            IsUnlockedAsNCG = false;
        }

        public void UnlockAsNCG()
        {
            itemButton.interactable = true;
            backgroundImage.overrideSprite = Resources.Load<Sprite>("UI/Textures/ui_box_Inventory_04");
            backgroundImage.SetNativeSize();
            ncgEffectImage.enabled = true;
            IsUnlockedAsNCG = true;
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
