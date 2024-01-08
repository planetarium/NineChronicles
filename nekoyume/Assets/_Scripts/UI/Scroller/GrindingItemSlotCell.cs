using Nekoyume.UI.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class GrindingItemSlotCell : GridCell<InventoryItem, GrindingItemSlotScroll.ContextModel>
    {
        [SerializeField]
        private Image itemImage;

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private GameObject selectedObject;

        [SerializeField]
        private GameObject noneObject;

        [SerializeField]
        private Button removeButton;

        private InventoryItem _itemModel;

        private static readonly int Register = Animator.StringToHash("Register");

        public override void Initialize()
        {
            base.Initialize();

            removeButton.onClick.AddListener(() => Context.OnClick.OnNext(_itemModel));
        }

        public override void UpdateContent(InventoryItem itemData)
        {
            _itemModel = itemData;

            var isRegister = itemData != null && itemData.ItemBase != null;
            if (isRegister)
            {
                itemImage.overrideSprite = itemData.ItemBase.GetIconSprite();
            }

            animator.SetBool(Register, isRegister);
            selectedObject.SetActive(isRegister);
            noneObject.SetActive(!isRegister);
        }
    }
}
