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

        public IObservable<Unit> OnClick => removeButton.OnClickAsObservable();
        public InventoryItem AssignedItem { get; private set; }

        public void UpdateSlot(InventoryItem item = null)
        {
            var isRegister = item != null;
            AssignedItem = item;
            selectedSlotObject.SetActive(isRegister);
            itemImage.overrideSprite = item?.ItemBase.GetIconSprite();
            animator.SetTrigger(isRegister ? "Register" : "UnRegister");
        }
    }
}
