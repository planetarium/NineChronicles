using Nekoyume.TableData;
using Nekoyume.UI.Scroller;
using UniRx;
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

        protected override void Awake()
        {
            base.Awake();

            equipmentOptionRecipe.OnOptionClick
                .Subscribe(tuple => OnSelectOption(tuple.Item1, tuple.Item2))
                .AddTo(gameObject);
        }

        public void SetData(EquipmentItemRecipeSheet.Row recipeRow)
        {
            gameObject.SetActive(true);
            confirmArea.SetActive(false);
            equipmentOptionRecipe.Show(recipeRow);
        }

        private void OnSelectOption(EquipmentRecipeCellView recipeView, EquipmentOptionRecipeView optionRecipeView)
        {
            SelectedSubRecipeId = optionRecipeView.SubRecipeId;
            equipmentOptionRecipe.gameObject.SetActive(false);
            SetData(recipeView.model, SelectedSubRecipeId);
            confirmArea.SetActive(true);
        }
    }
}
