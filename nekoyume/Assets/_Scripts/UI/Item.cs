using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Item : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject popUp;
        public Text itemName;
        public Text info;
        public Text price;

        public void OnPointerEnter(PointerEventData eventData)
        {
            popUp.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            popUp.SetActive(false);
        }
    }
}
