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

        public IObservable<Unit> OnClick => removeButton.OnClickAsObservable();
        public InventoryItem AssignedItem { get; private set; }

        public void UpdateSlot(InventoryItem item = null)
        {
            AssignedItem = item;
            selectedSlotObject.SetActive(item != null);
            itemImage.overrideSprite = item?.ItemBase.GetIconSprite();
        }
    }
}
