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
        [SerializeField] private GameObject emptyEffectContainer;
        [SerializeField] private GameObject stageEffectContainer;
        [SerializeField] private GameObject itemContainer;

        private System.Action _callback;

        private void Awake()
        {
            removeButton.onClick.AddListener(RemoveMaterial);
        }

        public void AddMaterial(ItemBase itemBase, System.Action callback)
        {
            _callback = callback;
            stageEffectContainer.SetActive(true);
            emptyEffectContainer.SetActive(false);
            plusContainer.SetActive(false);
            itemContainer.SetActive(true);
            itemImage.overrideSprite = itemBase.GetIconSprite();
        }

        public void RemoveMaterial()
        {
            _callback?.Invoke();
            _callback = null;
            stageEffectContainer.SetActive(false);
            emptyEffectContainer.SetActive(true);
            plusContainer.SetActive(true);
            itemContainer.SetActive(false);

        }
    }
}
