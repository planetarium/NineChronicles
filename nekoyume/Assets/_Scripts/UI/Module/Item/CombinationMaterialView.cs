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
            // 잠긴 배경. 버튼 비활성화.
//            backgroundImage.sprite
            Clear();
            itemButton.interactable = false;
        }
        
        public void Unlock()
        {
            // 열린 배경. 버튼 활성화.
//            backgroundImage.sprite
            itemButton.interactable = true;
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
