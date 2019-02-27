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
        public ItemBase item;

        public void OnPointerEnter(PointerEventData eventData)
        {
            button.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            button.gameObject.SetActive(false);
        }

        public void Equip(CartItem selected)
        {
            icon.sprite = selected.icon.sprite;
            icon.gameObject.SetActive(true);
            item = selected.item;
        }
        public void UnEquip()
        {
            icon.gameObject.SetActive(false);
            var _player = FindObjectOfType<Player>();
            _player.Equip((Equipment) item);
        }
    }
}

