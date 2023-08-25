using Nekoyume.Model.Item;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class UpgradeEquipmentSlot : MonoBehaviour
    {
        [SerializeField] private Button[] removeButtons;
        [SerializeField] private Image itemImage;
        [SerializeField] private GameObject plusContainer;
        [SerializeField] private GameObject emptyEffectContainer;
        [SerializeField] private GameObject stageEffectContainer;
        [SerializeField] private GameObject itemContainer;

        public bool IsExist { get; private set; }

        public void AddRemoveButtonAction(UnityAction removeAction)
        {
            foreach (var item in removeButtons)
            {
                item.onClick.AddListener(removeAction);
            }
        }

        public void AddMaterial(ItemBase itemBase)
        {
            stageEffectContainer.SetActive(true);
            emptyEffectContainer.SetActive(false);
            plusContainer.SetActive(false);
            itemContainer.SetActive(true);
            itemImage.overrideSprite = itemBase.GetIconSprite();
            IsExist = true;
        }

        public void RemoveMaterial()
        {
            stageEffectContainer.SetActive(false);
            emptyEffectContainer.SetActive(true);
            plusContainer.SetActive(true);
            itemContainer.SetActive(false);
            IsExist = false;
        }
    }
}
