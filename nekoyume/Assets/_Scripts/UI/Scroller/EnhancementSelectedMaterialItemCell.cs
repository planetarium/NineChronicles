using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class EnhancementSelectedMaterialItemCell : GridCell<Model.EnhancementInventoryItem,
        EnhancementSelectedMaterialItemScroll.ContextModel>
    {
        [SerializeField]
        private Image itemImage;

        [SerializeField]
        private Animator animator;

        private static readonly int Register = Animator.StringToHash("Register");

        public override void UpdateContent(Model.EnhancementInventoryItem viewModel)
        {
            if(viewModel == null || viewModel.ItemBase == null)
            {
                animator.SetBool(Register, false);
                return;
            }

            itemImage.overrideSprite = viewModel.ItemBase.GetIconSprite();
            animator.SetBool(Register, true);
        }

        public void ResetSlot()
        {
            animator.SetBool(Register, false);
        }
    }
}
