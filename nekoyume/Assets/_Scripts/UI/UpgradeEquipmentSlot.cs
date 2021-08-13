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

        public Button RemoveButton => removeButton;

        public void AddMaterial(ItemBase itemBase)
        {
            stageEffectContainer.SetActive(true);
            emptyEffectContainer.SetActive(false);
            plusContainer.SetActive(false);
            itemContainer.SetActive(true);
            itemImage.overrideSprite = itemBase.GetIconSprite();
        }

        public void RemoveMaterial()
        {
            stageEffectContainer.SetActive(false);
            emptyEffectContainer.SetActive(true);
            plusContainer.SetActive(true);
            itemContainer.SetActive(false);
        }
    }
}
