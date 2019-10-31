using Nekoyume.UI.Model;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CombinationMaterialView : CountEditableItemView<CombinationMaterial>
    {
        public Image[] effectImages;

        public bool IsLocked { get; private set; }

        public InventoryItem InventoryItemViewModel { get; private set; }

        public void Set(InventoryItemView inventoryItem)
        {
            if (inventoryItem is null ||
                inventoryItem.Model is null ||
                inventoryItem.Model.ItemBase.Value is null)
            {
                Clear();
                return;
            }

            var model = new CombinationMaterial(
                inventoryItem.Model.ItemBase.Value,
                1,
                1,
                inventoryItem.Model.Count.Value);
            base.SetData(model);
            SetEnableEffectImages(true);
            InventoryItemViewModel = inventoryItem.Model;
        }

        public override void Clear()
        {
            InventoryItemViewModel = null;
            SetEnableEffectImages(false);
            base.Clear();
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

        private void SetEnableEffectImages(bool enable)
        {
            foreach (var effectImage in effectImages)
            {
                effectImage.enabled = enable;
            }
        }
    }
}
