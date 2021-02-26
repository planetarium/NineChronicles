using Nekoyume.TableData;
using Nekoyume.UI.Tween;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class ElementalCombinationMaterialPanel : CombinationMaterialPanel
    {
        [SerializeField]
        private EquipmentOptionView optionView = null;

        [SerializeField]
        private DOTweenRectTransformMoveTo panelTweener = null;

        [SerializeField]
        private Animator animator = null;

        public void TweenPanel(EquipmentOptionRecipeView view)
        {
            var rect = view.transform as RectTransform;

            panelTweener.SetBeginRect(rect);
            panelTweener.Play();
            animator.Play("Show");
            animator.speed = 0f;
        }

        public override void SetData(
            EquipmentItemRecipeSheet.Row row,
            int? subRecipeId,
            bool checkInventory = true
        )
        {
            var equipmentSheet = Game.Game.instance.TableSheets.EquipmentItemSheet;
            if (!equipmentSheet.TryGetValue(row.ResultEquipmentId, out var equipmentRow))
            {
                Debug.LogWarning($"Equipment ID not found : {row.ResultEquipmentId}");
                return;
            }

            if (subRecipeId.HasValue)
            {
                optionView.Show(equipmentRow.GetLocalizedName(), subRecipeId.Value);
                optionView.SetDimmed(false);
            }
            else
            {
                optionView.Hide();
            }
            base.SetData(row, subRecipeId, checkInventory);
        }

        public void OnTweenCompleted()
        {
            animator.speed = 1f;
        }
    }
}
