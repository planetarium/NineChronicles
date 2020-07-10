using Nekoyume.TableData;
using Nekoyume.UI.Scroller;
using Nekoyume.UI.Tween;
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

        [SerializeField]
        protected DOTweenGroupAlpha optionAlphaTweener = null;

        [SerializeField]
        protected AnchoredPositionYTweener optionYTweener = null;

        [SerializeField]
        protected DOTweenRectTransformMoveTo optionCellViewTweener = null;

        protected override void Awake()
        {
            base.Awake();

            equipmentOptionRecipe.OnOptionClick
                .Subscribe(_ => Widget.Find<Combination>()?.OnTweenRecipe());

            equipmentOptionRecipe.OnOptionClickVFXCompleted
                .Subscribe(tuple => OnSelectOption(tuple.Item1, tuple.Item2))
                .AddTo(gameObject);
        }

        public void SetData(EquipmentItemRecipeSheet.Row recipeRow)
        {
            gameObject.SetActive(true);
            confirmArea.SetActive(false);
            equipmentOptionRecipe.Show(recipeRow);

            optionAlphaTweener.PlayDelayed(0.2f);
            optionYTweener.PlayTween();
        }

        private void OnSelectOption(EquipmentRecipeCellView recipeView, EquipmentOptionRecipeView optionRecipeView)
        {
            SelectedSubRecipeId = optionRecipeView.SubRecipeId;
            equipmentOptionRecipe.gameObject.SetActive(false);
            SetData(recipeView.RowData, SelectedSubRecipeId);
            confirmArea.SetActive(true);
            TweenCellView(recipeView, equipmentOptionRecipe.KillCellViewTween);

            if (materialPanel is ElementalCombinationMaterialPanel panel)
            {
                Widget.Find<Combination>().OnTweenRecipe();
                panel.TweenPanel(optionRecipeView);
            }
        }

        public void TweenCellViewInOption(RecipeCellView view, System.Action onCompleted)
        {
            var rect = view.transform as RectTransform;

            optionCellViewTweener.SetBeginRect(rect);
            optionCellViewTweener.onCompleted = onCompleted;
            optionCellViewTweener.Play();
        }

        public override void Hide()
        {
            equipmentOptionRecipe.KillCellViewTween();
            base.Hide();
        }
    }
}
