using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class ElementalCombinationPanel : EquipmentCombinationPanel
    {
        public int SelectedSubRecipeId { get; protected set; }

        [SerializeField]
        private EquipmentOptionRecipe equipmentOptionRecipe = null;

        [SerializeField]
        private GameObject confirmArea = null;


        public void SetData(EquipmentRecipeCellView view)
        {
            gameObject.SetActive(true);
            confirmArea.SetActive(false);
            equipmentOptionRecipe.SetData(view, OnSelectOption);
        }

        public void OnSelectOption(EquipmentRecipeCellView view, int subRecipeId)
        {
            SelectedSubRecipeId = subRecipeId;
            equipmentOptionRecipe.gameObject.SetActive(false);
            SetData(view, subRecipeId);
            confirmArea.SetActive(true);
        }
    }
}
