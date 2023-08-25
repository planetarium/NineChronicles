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

        [SerializeField]
        private GameObject seletedObj;

        [SerializeField]
        private Button removeButton;

        private Model.EnhancementInventoryItem item;

        private static readonly int Register = Animator.StringToHash("Register");

        public override void Initialize()
        {
            base.Initialize();
            removeButton.onClick.AddListener(() => Context.OnClick.OnNext(item));
        }

        public override void UpdateContent(Model.EnhancementInventoryItem viewModel)
        {
            item = viewModel;
            if(viewModel == null || viewModel.ItemBase == null)
            {
                animator.SetBool(Register, false);
                seletedObj.SetActive(false);
                return;
            }
            seletedObj.SetActive(true);
            animator.SetBool(Register, true);
            itemImage.overrideSprite = viewModel.ItemBase.GetIconSprite();
        }
    }
}
