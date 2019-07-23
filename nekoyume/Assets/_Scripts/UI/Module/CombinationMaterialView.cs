using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class CombinationMaterialView : CountEditableItemView<Model.CountEditableItem>
    {
        public void SetBackgroundImageToBaseEquipmentMaterial()
        {
            backgroundImage.sprite = Resources.Load<Sprite>("UI/Textures/UI_icon_item_combination_01");
        }

        public void SetBackgroundImageToEmpty()
        {
            backgroundImage.sprite = Resources.Load<Sprite>("UI/Textures/UI_box_02");
        }
    }
}
