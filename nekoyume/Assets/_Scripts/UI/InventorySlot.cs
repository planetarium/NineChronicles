using Nekoyume.Game.Controller;
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
        public GameObject glow;

        public ItemBase Item;

        private string _itemDir;
        private string _equipDir;

        public void Clear()
        {
            Icon.gameObject.SetActive(false);
            LabelCount.text = "";
            LabelEquip.text = "";
        }

        public void Set(ItemBase item, int count)
        {
            Item = item;
            Icon.sprite = ItemBase.GetSprite(item);
            Icon.gameObject.SetActive(true);
            Icon.SetNativeSize();

            LabelCount.text = count.ToString();
            LabelEquip.text = "";
            toggled = false;
            outLine.SetActive(toggled);
        }

        public void SlotClick()
        {
            toggled = !toggled;
            outLine.SetActive(toggled);
            Game.Event.OnSlotClick.Invoke(this, toggled);
            AudioController.PlaySelect();
        }

        public void SetAlpha(float alpha)
        {
            var color = Icon.color;
            color.a = alpha;
            Icon.color = color;
        }
    }
}
