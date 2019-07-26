using Nekoyume.Game.Item;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class EquipSlot : MonoBehaviour
    {
        public GameObject button;
        public Image icon;
        public ItemUsable item;
        public ItemBase.ItemType type;

        public void Unequip()
        {
            icon.overrideSprite = null;
            icon.SetNativeSize();
            item = null;
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }
        }

        public void Set(ItemUsable equipment)
        {
            var sprite = ItemBase.GetSprite(equipment);
            icon.overrideSprite = sprite;
            icon.gameObject.SetActive(true);
            icon.SetNativeSize();
            item = equipment;
            if (button != null)
            {
                button.gameObject.SetActive(true);
            }
        }
    }
}
