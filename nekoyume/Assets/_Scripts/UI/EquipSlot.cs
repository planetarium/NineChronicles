using Nekoyume.Game.Character;
using Nekoyume.Game.Item;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class EquipSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject button;
        public Image icon;
        public Equipment item;
        public ItemBase.ItemType type;


        public void OnPointerEnter(PointerEventData eventData)
        {
            button.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            button.gameObject.SetActive(false);
        }

        public void Equip(Player player, CartItem selected)
        {
            icon.sprite = selected.icon.sprite;
            icon.gameObject.SetActive(true);
            item = (Equipment) selected.item;
            player.Equip(item);
        }
        public void UnEquip(Player player)
        {
            icon.gameObject.SetActive(false);
            player.Equip(item);
        }

        public void Set(Player player, Equipment equipment)
        {
            var sprite = Resources.Load<Sprite>($"images/item_{equipment.Data.Id}");
            if (sprite == null)
                sprite = Resources.Load<Sprite>("images/item_301001");
            icon.sprite = sprite;
            icon.gameObject.SetActive(true);
            item = equipment;
        }
    }
}
