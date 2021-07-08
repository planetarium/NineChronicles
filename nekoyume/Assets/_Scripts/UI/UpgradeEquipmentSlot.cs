using Nekoyume.Helper;
using Nekoyume.Model.Item;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class UpgradeEquipmentSlot : MonoBehaviour
    {
        [SerializeField] private Button removeButton;
        [SerializeField] private Image itemImage;
        [SerializeField] private GameObject plusContainer;
        [SerializeField] private GameObject itemContainer;

        private System.Action _callback;

        private void Awake()
        {
            removeButton.onClick.AddListener(RemoveMaterial);
        }

        public void AddMaterial(ItemBase itemBase, System.Action callback)
        {
            _callback = callback;
            plusContainer.SetActive(false);
            itemContainer.SetActive(true);
            itemImage.overrideSprite = itemBase.GetIconSprite();
        }

        public void RemoveMaterial()
        {
            _callback?.Invoke();
            _callback = null;
            plusContainer.SetActive(true);
            itemContainer.SetActive(false);

        }
    }
}
