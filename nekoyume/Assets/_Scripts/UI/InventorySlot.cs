using Nekoyume.Game.Item;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    public class InventorySlot : MonoBehaviour
    {
        public Image Icon;
        public Text LabelCount;
        public Text LabelEquip;
        public bool toggled;
        public GameObject outLine;

        public ItemBase Item;

        public void Clear()
        {
            Icon.gameObject.SetActive(false);
            LabelCount.text = "";
            LabelEquip.text = "";
        }

        public void Set(ItemBase item, int count)
        {
            var sprite = Resources.Load<Sprite>($"images/item_{item.Data.Id}");
            if (sprite == null)
                sprite = Resources.Load<Sprite>("images/item_301001");
            Icon.sprite = sprite;
            Icon.gameObject.SetActive(true);
            Icon.SetNativeSize();
            LabelCount.text = count.ToString();
            LabelEquip.text = "";
            Item = item;
            toggled = false;
            outLine.SetActive(toggled);
        }

        public void SlotClick()
        {
            toggled = !toggled;
            outLine.SetActive(toggled);
            Game.Event.OnSlotClick.Invoke(this);
        }
    }
}
