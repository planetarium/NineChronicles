using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class EquipSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject button;
        public Image icon;

        public void OnPointerEnter(PointerEventData eventData)
        {
            button.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            button.gameObject.SetActive(false);
        }
    }
}

