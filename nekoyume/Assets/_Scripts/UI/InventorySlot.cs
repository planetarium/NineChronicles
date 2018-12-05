using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    public class InventorySlot : MonoBehaviour
    {
        public Image Icon;
        public Text LabelCount;

        public void Clear()
        {
            Icon.gameObject.SetActive(false);
            LabelCount.text = "";
        }

        public void Set(string itemId, int count)
        {
            var sprite = Resources.Load<Sprite>($"images/item_{itemId}");
            Icon.sprite = sprite;
            Icon.gameObject.SetActive(true);
            Icon.SetNativeSize();
            LabelCount.text = count.ToString();
        }
    }
}
