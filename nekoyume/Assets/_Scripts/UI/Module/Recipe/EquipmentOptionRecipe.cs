using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class EquipmentOptionRecipe : MonoBehaviour
    {
        [SerializeField]
        private EquipmentRecipeCellView equipmentRecipeCellView = null;

        [SerializeField]
        private EquipmentOptionRecipeView[] equipmentOptionRecipeViews = null;

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Show(int recipe)
        {
            Show();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
