using System;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class GrindingItemSlot : MonoBehaviour
    {
        [SerializeField]
        private Button removeButton;

        [SerializeField]
        private Image itemImage;

        [SerializeField]
        private GameObject selectedSlotObject;

        [SerializeField]
        private Animator animator;

        private static readonly int Register = Animator.StringToHash("Register");

        public IObservable<Unit> OnClick => removeButton.OnClickAsObservable();
        public InventoryItem AssignedItem { get; private set; }

        public void UpdateSlot(InventoryItem item = null)
        {
            var isRegister = item != null;
            AssignedItem = item;
            if (isRegister)
            {
                itemImage.overrideSprite = item.ItemBase.GetIconSprite();
            }

            animator.SetBool(Register, isRegister);
        }

        public void ResetSlot()
        {
            AssignedItem = null;
            animator.SetBool(Register, false);
            selectedSlotObject.SetActive(false);
        }
    }
}
