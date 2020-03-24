using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Scroller;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class EquipmentOptionRecipe : MonoBehaviour
    {
        [SerializeField]
        private EquipmentRecipeCellView equipmentRecipeCellView = null;

        [SerializeField]
        private EquipmentOptionRecipeView[] equipmentOptionRecipeViews = null;

        public readonly Subject<(EquipmentRecipeCellView, EquipmentOptionRecipeView)> OnOptionClick =
            new Subject<(EquipmentRecipeCellView, EquipmentOptionRecipeView)>();

        private void Awake()
        {
            foreach (var view in equipmentOptionRecipeViews)
            {
                if (view is null)
                {
                    throw new SerializeFieldNullException();
                }

                view.OnClick
                    .Subscribe(item => OnOptionClick.OnNext((equipmentRecipeCellView, item)))
                    .AddTo(gameObject);
            }
        }

        private void OnDestroy()
        {
            OnOptionClick.Dispose();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Show(EquipmentItemRecipeSheet.Row recipeRow)
        {
            equipmentRecipeCellView.Set(recipeRow);
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
                    new EquipmentItemSubRecipeSheet.MaterialInfo(recipeRow.MaterialId, recipeRow.MaterialCount));
            }

            UpdateOptionRecipes();
        }

        private void UpdateOptionRecipes()
        {
            var avatarState = States.Instance.CurrentAvatarState;
            if (avatarState is null)
                return;

            foreach (var recipeView in equipmentOptionRecipeViews)
            {
                recipeView.Set(avatarState);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
