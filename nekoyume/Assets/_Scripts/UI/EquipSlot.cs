using Nekoyume.Game.Character;
using Nekoyume.Game.Item;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class EquipSlot : MonoBehaviour
    {
        public GameObject button;
        public Image icon;
        public Equipment item;
        public ItemBase.ItemType type;



        public void Equip(SelectedItem selected)
        {
            icon.sprite = selected.icon.sprite;
            icon.gameObject.SetActive(true);
            item = (Equipment) selected.item;
            if (button != null)
            {
                button.gameObject.SetActive(true);
            }
        }
        public void Unequip()
        {
            icon.gameObject.SetActive(false);
            item = null;
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }
        }

        public void Set(Equipment equipment)
        {
            var sprite = Resources.Load<Sprite>($"images/item_{equipment.Data.Id}");
            if (sprite == null)
                sprite = Resources.Load<Sprite>("images/item_301001");
            icon.sprite = sprite;
            icon.gameObject.SetActive(true);
            item = equipment;
            if (button != null)
            {
                button.gameObject.SetActive(true);
            }
        }
    }
}
