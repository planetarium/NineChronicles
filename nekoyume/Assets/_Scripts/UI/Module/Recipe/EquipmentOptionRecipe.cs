using Nekoyume.Model.Quest;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Scroller;
using System;
using System.Linq;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class EquipmentOptionRecipe : MonoBehaviour
    {
        [SerializeField]
        private RecipeCellView recipeCellView = null;

        [SerializeField]
        private EquipmentOptionRecipeView[] equipmentOptionRecipeViews = null;

        public readonly Subject<Unit> OnOptionClick = new Subject<Unit>();

        public readonly Subject<(RecipeCellView recipeCellView, EquipmentOptionRecipeView item)> OnOptionClickVFXCompleted =
            new Subject<(RecipeCellView, EquipmentOptionRecipeView)>();

        protected int _recipeId;

        private void Awake()
        {
            foreach (var view in equipmentOptionRecipeViews)
            {
                if (view is null)
                {
                    throw new SerializeFieldNullException();
                }

                view.OnClick.Subscribe(_ => OnOptionClick.OnNext(_));
                view.OnClickVFXCompleted
                    .Subscribe(item => OnOptionClickVFXCompleted.OnNext((recipeCellView, item)))
                    .AddTo(gameObject);
            }
        }

        private void OnDestroy()
        {
            OnOptionClickVFXCompleted.Dispose();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Show(EquipmentItemRecipeSheet.Row recipeRow)
        {
            _recipeId = recipeRow.Id;
            recipeCellView.Set(recipeRow);
            InitializeOptionRecipes(recipeRow);
            Show();
        }

        private void InitializeOptionRecipes(EquipmentItemRecipeSheet.Row recipeRow)
        {
            for (var i = 0; i < equipmentOptionRecipeViews.Length; ++i)
            {
                var optionRecipeView = equipmentOptionRecipeViews[i];
                if (i >= recipeRow.SubRecipeIds.Count)
                {
                    optionRecipeView.ShowLocked();
                    continue;
                }

                var subRecipeId = recipeRow.SubRecipeIds[i];
                var equipmentSheet = Game.Game.instance.TableSheets.EquipmentItemSheet;
                if (!equipmentSheet.TryGetValue(recipeRow.ResultEquipmentId, out var row))
                {
                    Debug.LogWarning($"Equipment ID not found : {recipeRow.ResultEquipmentId}");
                    Hide();
                    return;
                }

                optionRecipeView.Show(
                    row.GetLocalizedName(),
                    subRecipeId,
                    new EquipmentItemSubRecipeSheet.MaterialInfo(recipeRow.MaterialId, recipeRow.MaterialCount),
                    true,
                    (_recipeId, i));
            }

            UpdateOptionRecipes();
        }

        public void KillCellViewTween()
        {
            foreach (var view in equipmentOptionRecipeViews)
            {
                view.shakeTweener.KillTween();
            }
        }

        private void UpdateOptionRecipes()
        {
            var avatarState = States.Instance.CurrentAvatarState;
            if (avatarState is null)
            {
                return;
            }

            foreach (var recipeView in equipmentOptionRecipeViews)
            {
                var isFirstOpen =
                    !Widget.Find<Combination>()
                    .RecipeVFXSkipMap[_recipeId]
                    .Contains(recipeView.SubRecipeId);

                recipeView.Set(avatarState, isFirstOpen);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
