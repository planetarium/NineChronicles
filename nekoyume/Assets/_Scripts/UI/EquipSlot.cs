using Nekoyume.Game.Item;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class EquipSlot : MonoBehaviour
    {
        public GameObject button;
        public Image defaultImage;
        public Image itemImage;
        public ItemUsable item;
        public ItemBase.ItemType type;

        public void Unequip()
        {
            if (defaultImage)
            {
                defaultImage.enabled = true;    
            }
            itemImage.enabled = false;
            item = null;
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }
        }

        public void Set(ItemUsable equipment)
        {
            var sprite = ItemBase.GetSprite(equipment);
            if (defaultImage)
            {
                defaultImage.enabled = false;
            }
            itemImage.enabled = true;
            itemImage.overrideSprite = sprite;
            itemImage.SetNativeSize();
            item = equipment;
            if (button != null)
            {
                button.gameObject.SetActive(true);
            }
        }
    }
}
